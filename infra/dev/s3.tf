# chaucer-openlib-version
resource "aws_s3_bucket" "chaucer-openlib-version" {
  bucket = "${var.openlib-versions}"
  acl    = "private"

  tags = {
    environment = "${var.env}"
  }
}
