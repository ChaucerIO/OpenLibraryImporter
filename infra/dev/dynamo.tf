resource "aws_dynamodb_table" "chaucer-openlib-versions" {
  name           = "${var.openlib-versions}"
  billing_mode   = "PROVISIONED"
  read_capacity  = 5
  write_capacity = 5
  hash_key       = "Kind"
  range_key      = "PublishDate"

  attribute {
    name = "Kind"
    type = "S"
  }

  attribute {
    name = "PublishDate"
    type = "S"
  }

  tags = {
    Environment = "${var.env}"
  }
}