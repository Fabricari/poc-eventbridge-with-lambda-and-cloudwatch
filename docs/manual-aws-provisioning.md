# Manual AWS Infrastructure Provisioning

This guide walks through provisioning the three AWS resources shown in the architecture diagram using the AWS Management Console.

![Architecture reference](architecture/poc-eventbridge-lambda-cloudwatch.jpg)

All steps assume you are signed in to the AWS Console and working in a single region.

---

## Prerequisites

- An AWS account with permissions to create Lambda functions, IAM roles, and EventBridge resources.
- The deployment packages already built in the `packages/` folder:
  - `packages/MessageSubmissionLambda/MessageSubmissionLambda.zip`
  - `packages/MessageModerationLambda/MessageModerationLambda.zip`

---

## 1. Create the Custom Event Bus

1. Open the **Amazon EventBridge Console** > **Event buses**.
2. Choose **Create event bus**.
3. Complete the form:

| Section | Setting | Value |
|---|---|---|
| Event bus details | Name | `message-moderation-bus` |
| Encryption | Encryption type | **Use AWS owned key** (default) |
| Logs | Log destinations | **No log destinations selected** (default) |

4. Choose **Create**.
5. Copy the event bus ARN for later steps (for example, `arn:aws:events:<region>:<account-id>:event-bus/message-moderation-bus`).

---

## 2. Create IAM Execution Roles

Each Lambda function assumes an IAM execution role that controls which AWS services the function itself can call. The publisher Lambda needs permission to write events to EventBridge, while the subscriber Lambda only needs the default permission to write logs to CloudWatch. Separate roles are shown below to keep these permissions scoped to each function.

### Publisher Role (MessageSubmissionLambda)

1. Open the **IAM Console** > **Roles** > **Create role**.
2. In **Select trusted entity**:
  - Trusted entity type: **AWS service**
  - Service or use case: **Lambda**
  - Use case: **Lambda (Default)**
  - Choose **Next**
3. In **Add permissions**:
  - Attach policy: **AWSLambdaBasicExecutionRole**
  - Choose **Next**
4. In **Name, review, and create**:
  - Role name: `MessageSubmissionLambdaRole`
  - Choose **Create role**
5. After the role is created, add an inline policy:
  - Choose **Add permissions** > **Create inline policy**
  - Service: **EventBridge**
  - Actions allowed: **PutEvents**
  - Resources: **Add ARNs**
  - Resource ARN: paste the event bus ARN from step 1
  - Choose **Add ARNs**
  - Choose **Next**
6. In **Review and create**:
  - Policy name: `AllowEventBridgePutEventsPolicy`
  - Choose **Create policy**
7. Confirm the role now has both policies attached:
  - `AWSLambdaBasicExecutionRole`
  - `AllowEventBridgePutEventsPolicy`

### Subscriber Role (MessageModerationLambda)

1. Open the **IAM Console** > **Roles** > **Create role**.
2. In **Select trusted entity**:
  - Trusted entity type: **AWS service**
  - Service or use case: **Lambda**
  - Use case: **Lambda (Default)**
  - Choose **Next**
3. In **Add permissions**:
  - Attach policy: **AWSLambdaBasicExecutionRole**
  - Choose **Next**
4. In **Name, review, and create**:
  - Role name: `MessageModerationLambdaRole`
  - Choose **Create role**

No additional inline policies are required for this subscriber role.

---

## 3. Create the Lambda Functions

### MessageSubmissionLambda (Publisher with Function URL)

1. Open the **Lambda Console** > **Functions** > **Create function**.
2. Keep **Author from scratch** selected.
3. In **Basic information**:

| Setting | Value |
|---|---|
| Function name | `MessageSubmissionLambda` |
| Runtime | **.NET 10 (C#/F#/PowerShell)** |

4. In **Additional settings**:
  - Under **Execution role**, select **Use an existing role** and choose `MessageSubmissionLambdaRole`, then choose **Save**
  - Under **Function URL**, set:
    - Auth type: **NONE**
    - Invoke mode: **BUFFERED** (default)
    - Choose **Save**
5. Choose **Create function**.
6. Copy the generated Function URL for testing.
7. Upload the deployment package:
  - **Code** tab > **Upload from** > **.zip file**
  - Select `packages/MessageSubmissionLambda/MessageSubmissionLambda.zip`
  - Choose **Save**
8. Under **Runtime settings**, choose **Edit** and set the handler:

```
MessageSubmissionLambda::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler
```

Then choose **Save**.

9. Under **Configuration** > **Environment variables** > **Edit**, add:

| Key | Value |
|---|---|
| `EVENT_BUS_NAME` | `message-moderation-bus` |
| `EVENT_SOURCE` | `message-submission-service` |
| `EVENT_DETAIL_TYPE` | `MessageSubmitted` |

Then choose **Save**.

### MessageModerationLambda (Subscriber)

1. Open the **Lambda Console** > **Functions** > **Create function**.
2. Keep **Author from scratch** selected.
3. In **Basic information**:

| Setting | Value |
|---|---|
| Function name | `MessageModerationLambda` |
| Runtime | **.NET 10 (C#/F#/PowerShell)** |

4. In **Additional settings**, set **Execution role** to **Use an existing role** and choose `MessageModerationLambdaRole`, then choose **Save**.
5. Choose **Create function**.
6. Upload the deployment package:
  - **Code** tab > **Upload from** > **.zip file**
  - Select `packages/MessageModerationLambda/MessageModerationLambda.zip`
  - Choose **Save**
7. Under **Runtime settings**, choose **Edit** and set the handler:

```
MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler
```

Then choose **Save**.

No environment variables are required for this function.

---

## 4. Create the EventBridge Rule

The rule matches events published by the submission Lambda and routes them to the moderation Lambda.

1. Open the **EventBridge Console** > **Rules** > **Create rule**.
2. If **Visual rule builder** is selected, deselect it.
3. In **Define rule detail**, set:

| Setting | Value |
|---|---|
| Name | `route-to-moderation-lambda` |
| Event bus | `message-moderation-bus` |
| Rule state | **Enabled** |

Then choose **Next**.

4. In **Build event pattern**:
  - Select **Custom pattern (JSON editor)**
  - Enter:

```json
{
  "source": ["message-submission-service"],
  "detail-type": ["MessageSubmitted"]
}
```

  - Choose **Next**

These values must match `EVENT_SOURCE` and `EVENT_DETAIL_TYPE` from the publisher Lambda.

5. In **Select target(s)**:
  - Target types: **AWS service**
  - Select a target: **Lambda function**
  - Function: **MessageModerationLambda**
  - Choose **Next**
6. In **Configure tags**, choose **Next**.
7. In **Review and create**, choose **Create rule**.
8. Copy the rule ARN for the Lambda permission step.

---

## 5. Verify the Lambda Resource-Based Policy

The execution roles in step 2 control what each Lambda can call out to. This step covers the reverse direction: the resource-based policy on the moderation Lambda controls what is allowed to invoke it.

1. Open **MessageModerationLambda** in the Lambda Console.
2. Go to **Configuration** > **Permissions**.
3. Under **Resource-based policy statements**, verify the list is currently empty.
4. Choose **Add permission**.
5. In **Edit policy statement**, set:
  - Policy statement type: **AWS service**
  - Service: **EventBridge (CloudWatch Events)**
  - Statement ID: `AllowEventBridgeInvoke`
  - Principal: `events.amazonaws.com` (default)
  - Source ARN: the EventBridge rule ARN from step 4 (not the event bus ARN)
  - Action: `lambda:InvokeFunction`
6. Choose **Save**.

Rule ARN format:

```
arn:aws:events:<region>:<account-id>:rule/message-moderation-bus/route-to-moderation-lambda
```

Equivalent policy statement:

```json
{
  "Effect": "Allow",
  "Principal": {
    "Service": "events.amazonaws.com"
  },
  "Action": "lambda:InvokeFunction",
  "Resource": "arn:aws:lambda:<region>:<account-id>:function:MessageModerationLambda",
  "Condition": {
    "ArnLike": {
      "AWS:SourceArn": "arn:aws:events:<region>:<account-id>:rule/message-moderation-bus/route-to-moderation-lambda"
    }
  }
}
```

---

## 6. Test the End-to-End Flow

1. Start with the base Function URL copied from **MessageSubmissionLambda**:

```
https://<function-url-id>.lambda-url.<region>.on.aws/
```

2. Add a query string parameter for the message text:

| Query key | Query value |
|---|---|
| `text` | `hello world` |

Example full URL:

```
https://<function-url-id>.lambda-url.<region>.on.aws/?text=hello+world
```

3. Expect a `200` response: **"Message handed off for moderation."**

4. Check logs from each Lambda function page:
  - Open **MessageSubmissionLambda** > **Monitor** > **View CloudWatch logs**
  - Open **MessageModerationLambda** > **Monitor** > **View CloudWatch logs**
5. In the CloudWatch log view, clear date/time filters first, then apply filter text `DEMO`.
6. Verify expected log output in both groups:
  - `/aws/lambda/MessageSubmissionLambda` should include `DEMO | MESSAGE SUBMISSION`
  - `/aws/lambda/MessageModerationLambda` should include `DEMO | MESSAGE MODERATION`

### Additional Test Query Values

Use the same base Function URL and change only the `text` query value. These test cases cover every flagged term in the moderation word list.

```
gee+golly+I+did+not+expect+that
oh+gosh+drat+I+dropped+my+keys
rats+I+knew+I+should+have+turned+left
aw+shoot+shucks+that+was+my+last+chance
darn+it+dang+the+printer+is+jammed+again
what+the+heck+is+going+on+with+this+frick+thing
oh+fudge+I+left+the+oven+on
crud+the+build+broke+and+everything+is+crap
some+jerk+called+me+an+idiot+at+the+store
get+your+butt+over+here+you+left+poop+on+the+floor
```

Each test should return `200`, and the moderation Lambda logs should show `Flagged` status with matched terms listed alphabetically.

---

## Configuration Reference

| Resource | Key Setting | Value |
|---|---|---|
| Publisher Lambda | Function name | `MessageSubmissionLambda` |
| Publisher Lambda | Runtime | `dotnet10` |
| Publisher Lambda | Handler | `MessageSubmissionLambda::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler` |
| Publisher Lambda | Env: `EVENT_BUS_NAME` | `message-moderation-bus` |
| Publisher Lambda | Env: `EVENT_SOURCE` | `message-submission-service` |
| Publisher Lambda | Env: `EVENT_DETAIL_TYPE` | `MessageSubmitted` |
| Publisher Lambda | Function URL auth type | `NONE` |
| Publisher Lambda | Function URL invoke mode | `BUFFERED` |
| Subscriber Lambda | Function name | `MessageModerationLambda` |
| Subscriber Lambda | Runtime | `dotnet10` |
| Subscriber Lambda | Handler | `MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler` |
| Event Bus | Name | `message-moderation-bus` |
| EventBridge Rule | Name | `route-to-moderation-lambda` |
| EventBridge Rule | Rule state | `Enabled` |
| EventBridge Rule | Event pattern source | `message-submission-service` |
| EventBridge Rule | Event pattern detail-type | `MessageSubmitted` |
| EventBridge Rule | Target | `MessageModerationLambda` |
