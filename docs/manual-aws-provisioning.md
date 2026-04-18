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

1. Open the **Amazon EventBridge Console** > **Event buses** > **Create event bus**.

| Setting | Value |
|---|---|
| Name | `message-moderation-bus` |
| Encryption | **Use AWS owned key** (default) |

2. Leave all optional settings (archive, schema discovery, resource policy) at their defaults.
3. Choose **Create**.

---

## 2. Create IAM Execution Roles

Each Lambda function assumes an IAM execution role that controls which AWS services the function itself can call. The publisher Lambda needs permission to write events to EventBridge, while the subscriber Lambda only needs the default permission to write logs to CloudWatch. Separate roles are shown below to keep these permissions scoped to each function.

### Publisher Role (MessageSubmissionLambda)

1. Open the **IAM Console** > **Roles** > **Create role**.
2. Trusted entity type: **AWS service**.
3. Use case: **Lambda**.
4. Attach the managed policy **AWSLambdaBasicExecutionRole**.
5. Add an inline policy granting `events:PutEvents` on the custom event bus ARN created in step 1:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "events:PutEvents",
      "Resource": "arn:aws:events:<region>:<account-id>:event-bus/message-moderation-bus"
    }
  ]
}
```

6. Name the role (e.g., `MessageSubmissionLambdaRole`).

### Subscriber Role (MessageModerationLambda)

1. Repeat the same steps but attach only **AWSLambdaBasicExecutionRole**.
2. No additional inline policies are needed — this function only writes to CloudWatch Logs.
3. Name the role (e.g., `MessageModerationLambdaRole`).

---

## 3. Create the Lambda Functions

### MessageSubmissionLambda (Publisher with Function URL)

1. Open the **Lambda Console** > **Create function**.
2. Choose **Author from scratch**.

| Setting | Value |
|---|---|
| Function name | `MessageSubmissionLambda` |
| Runtime | **.NET 10 (dotnet10)** |
| Architecture | **x86_64** |
| Execution role | Use the publisher role created above |

3. After creation, upload the deployment package:
   - **Code** tab > **Upload from** > **.zip file**.
   - Select `packages/MessageSubmissionLambda/MessageSubmissionLambda.zip`.

4. Under **Runtime settings**, set the handler:

```
MessageSubmissionLambda::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler
```

5. Under **Configuration** > **Environment variables**, add:

| Key | Value |
|---|---|
| `EVENT_BUS_NAME` | `message-moderation-bus` |
| `EVENT_SOURCE` | `message-submission-service` |
| `EVENT_DETAIL_TYPE` | `MessageSubmitted` |

6. Enable a **Function URL**:
   - **Configuration** > **Function URL** > **Create function URL**.
   - Auth type: **NONE** (open access for demo purposes).
   - Save and copy the generated Function URL — this is the browser endpoint.

### MessageModerationLambda (Subscriber)

1. Open the **Lambda Console** > **Create function**.
2. Choose **Author from scratch**.

| Setting | Value |
|---|---|
| Function name | `MessageModerationLambda` |
| Runtime | **.NET 10 (dotnet10)** |
| Architecture | **x86_64** |
| Execution role | Use the subscriber role created above |

3. Upload the deployment package:
   - **Code** tab > **Upload from** > **.zip file**.
   - Select `packages/MessageModerationLambda/MessageModerationLambda.zip`.

4. Under **Runtime settings**, set the handler:

```
MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler
```

5. No environment variables are required for this function.

---

## 4. Create the EventBridge Rule

The rule matches events published by the submission Lambda and routes them to the moderation Lambda.

1. In the **EventBridge Console**, select the **message-moderation-bus** bus.
2. Go to **Rules** > **Create rule**.

| Setting | Value |
|---|---|
| Name | `route-to-moderation-lambda` |
| Event bus | `message-moderation-bus` |
| Rule type | **Rule with an event pattern** |

3. Define the event pattern. Use **Custom pattern (JSON editor)** and enter:

```json
{
  "source": ["message-submission-service"],
  "detail-type": ["MessageSubmitted"]
}
```

These values must match the `EVENT_SOURCE` and `EVENT_DETAIL_TYPE` environment variables configured on the publisher Lambda.

4. Set the target:
   - Target type: **AWS service**.
   - Select target: **Lambda function**.
   - Function: **MessageModerationLambda**.

5. Leave retry policy and dead-letter queue at defaults for this demo.
6. Choose **Create rule**.

---

## 5. Verify the Lambda Resource-Based Policy

The execution roles in step 2 control what each Lambda can call out to. This step covers the reverse direction — the resource-based policy on the moderation Lambda controls what is allowed to invoke it. When you selected the Lambda as a rule target in step 4, the console should have automatically added a policy granting EventBridge permission to invoke the function. Confirm this:

1. Open **MessageModerationLambda** in the Lambda Console.
2. Go to **Configuration** > **Permissions**.
3. Under **Resource-based policy statements**, verify an entry exists allowing `lambda:InvokeFunction` from the `events.amazonaws.com` service principal.

If the statement is missing, add it from the current console UI:

1. Choose **Add permissions**.
2. In **Edit policy statement**, choose **AWS service**.
3. Service: **EventBridge (CloudWatch Events)**.
4. Statement ID: use a value such as `AllowEventBridgeInvoke`.
5. Action: `lambda:InvokeFunction`.
6. Source ARN: use the **rule ARN**, not the event bus ARN:

```
arn:aws:events:<region>:<account-id>:rule/message-moderation-bus/route-to-moderation-lambda
```

7. Choose **Save**.

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

1. Open the Function URL for **MessageSubmissionLambda** in a browser with a `text` query parameter:

```
https://<function-url-id>.lambda-url.<region>.on.aws/?text=hello+world
```

2. Expect a `200` response: **"Message handed off for moderation."**

3. Open **CloudWatch Logs** and check the log groups for both functions:
   - `/aws/lambda/MessageSubmissionLambda` — look for `DEMO | MESSAGE SUBMISSION` log lines.
   - `/aws/lambda/MessageModerationLambda` — look for `DEMO | MESSAGE MODERATION` log lines.

### Additional Test URLs

These URLs cover every flagged term in the moderation word list. Each should return `200` and produce a `Flagged` status in the moderation Lambda's CloudWatch logs.

```
https://<function-url-id>.lambda-url.<region>.on.aws/?text=gee+golly+I+did+not+expect+that
https://<function-url-id>.lambda-url.<region>.on.aws/?text=oh+gosh+drat+I+dropped+my+keys
https://<function-url-id>.lambda-url.<region>.on.aws/?text=rats+I+knew+I+should+have+turned+left
https://<function-url-id>.lambda-url.<region>.on.aws/?text=aw+shoot+shucks+that+was+my+last+chance
https://<function-url-id>.lambda-url.<region>.on.aws/?text=darn+it+dang+the+printer+is+jammed+again
https://<function-url-id>.lambda-url.<region>.on.aws/?text=what+the+heck+is+going+on+with+this+frick+thing
https://<function-url-id>.lambda-url.<region>.on.aws/?text=oh+fudge+I+left+the+oven+on
https://<function-url-id>.lambda-url.<region>.on.aws/?text=crud+the+build+broke+and+everything+is+crap
https://<function-url-id>.lambda-url.<region>.on.aws/?text=some+jerk+called+me+an+idiot+at+the+store
https://<function-url-id>.lambda-url.<region>.on.aws/?text=get+your+butt+over+here+you+left+poop+on+the+floor
```

Together these sentences cover all 18 flagged terms. Each URL should return `200`, and the moderation Lambda's CloudWatch logs should show `Flagged` status with the matched terms listed alphabetically.

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
| Subscriber Lambda | Function name | `MessageModerationLambda` |
| Subscriber Lambda | Runtime | `dotnet10` |
| Subscriber Lambda | Handler | `MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler` |
| Event Bus | Name | `message-moderation-bus` |
| EventBridge Rule | Name | `route-to-moderation-lambda` |
| EventBridge Rule | Event pattern source | `message-submission-service` |
| EventBridge Rule | Event pattern detail-type | `MessageSubmitted` |
| EventBridge Rule | Target | `MessageModerationLambda` |
