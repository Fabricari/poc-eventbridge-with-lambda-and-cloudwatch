terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  event_bus_name    = "message-moderation-bus-tofu-test"
  event_source      = "message-submission-service"
  event_detail_type = "MessageSubmitted"

  submission_lambda_name = "MessageSubmissionLambdaTofuTest"
  moderation_lambda_name = "MessageModerationLambdaTofuTest"

  submission_role_name = "MessageSubmissionLambdaRoleTofuTest"
  moderation_role_name = "MessageModerationLambdaRoleTofuTest"

  event_rule_name = "route-to-moderation-lambda-tofu-test"

  submission_zip_path = "${path.module}/../packages/MessageSubmissionLambda/MessageSubmissionLambda.zip"
  moderation_zip_path = "${path.module}/../packages/MessageModerationLambda/MessageModerationLambda.zip"
}

resource "aws_cloudwatch_event_bus" "message_moderation_bus" {
  name = local.event_bus_name
}

resource "aws_iam_role" "message_submission_lambda_role" {
  name = local.submission_role_name

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
      Action = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role" "message_moderation_lambda_role" {
  name = local.moderation_role_name

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        Service = "lambda.amazonaws.com"
      }
      Action = "sts:AssumeRole"
    }]
  })
}

resource "aws_iam_role_policy_attachment" "submission_basic_execution" {
  role       = aws_iam_role.message_submission_lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy_attachment" "moderation_basic_execution" {
  role       = aws_iam_role.message_moderation_lambda_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

resource "aws_iam_role_policy" "allow_eventbridge_put_events_policy" {
  name = "AllowEventBridgePutEventsPolicyTofuTest"
  role = aws_iam_role.message_submission_lambda_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = "events:PutEvents"
      Resource = aws_cloudwatch_event_bus.message_moderation_bus.arn
    }]
  })
}

resource "aws_lambda_function" "message_submission_lambda" {
  function_name = local.submission_lambda_name
  role          = aws_iam_role.message_submission_lambda_role.arn
  runtime       = "dotnet8"
  handler       = "MessageSubmissionLambda::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler"

  timeout     = 15
  memory_size = 256

  filename         = local.submission_zip_path
  source_code_hash = filebase64sha256(local.submission_zip_path)

  environment {
  variables = {
    EVENT_BUS_NAME    = aws_cloudwatch_event_bus.message_moderation_bus.arn
    EVENT_SOURCE      = local.event_source
    EVENT_DETAIL_TYPE = local.event_detail_type
  }
}

  depends_on = [
    aws_iam_role_policy_attachment.submission_basic_execution,
    aws_iam_role_policy.allow_eventbridge_put_events_policy
  ]
}

resource "aws_lambda_function" "message_moderation_lambda" {
  function_name = local.moderation_lambda_name
  role          = aws_iam_role.message_moderation_lambda_role.arn
  runtime       = "dotnet8"
  handler       = "MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler"

  timeout     = 15
  memory_size = 256

  filename         = local.moderation_zip_path
  source_code_hash = filebase64sha256(local.moderation_zip_path)

  depends_on = [
    aws_iam_role_policy_attachment.moderation_basic_execution
  ]
}

resource "aws_lambda_function_url" "message_submission_function_url" {
  function_name      = aws_lambda_function.message_submission_lambda.function_name
  authorization_type = "NONE"
}

resource "aws_lambda_permission" "allow_public_function_url" {
  statement_id        = "AllowPublicFunctionUrlInvokeTofuTest"
  action              = "lambda:InvokeFunctionUrl"
  function_name       = aws_lambda_function.message_submission_lambda.function_name
  principal           = "*"
  function_url_auth_type = "NONE"
}

resource "aws_lambda_permission" "allow_public_invoke_function" {
  statement_id  = "AllowPublicInvokeFunctionTofuTest"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.message_submission_lambda.function_name
  principal     = "*"
}

resource "aws_cloudwatch_event_rule" "route_to_moderation_lambda" {
  name           = local.event_rule_name
  event_bus_name = aws_cloudwatch_event_bus.message_moderation_bus.name

  event_pattern = jsonencode({
    source      = [local.event_source]
    detail-type = [local.event_detail_type]
  })
}

resource "aws_cloudwatch_event_target" "moderation_lambda_target" {
  rule           = aws_cloudwatch_event_rule.route_to_moderation_lambda.name
  event_bus_name = aws_cloudwatch_event_bus.message_moderation_bus.name
  arn            = aws_lambda_function.message_moderation_lambda.arn
}

resource "aws_lambda_permission" "allow_eventbridge_invoke" {
  statement_id  = "AllowEventBridgeInvokeTofuTest"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.message_moderation_lambda.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.route_to_moderation_lambda.arn
}