#--------------------------------------------------------------
# Backend configuration
#--------------------------------------------------------------

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 3.27"
    }
  }

  required_version = ">= 0.14.9"

  backend "s3" {
    bucket = "chaucer-tfstate"
    key    = "dev-openlib-importer.tfstate"
    region = "us-east-2"
  }
}

provider "aws" {
  profile = "chaucer-tform"
  region  = "us-east-2"
}

data "aws_caller_identity" "current" {}
