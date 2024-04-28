#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $DIR/..
ROOT=$(pwd)
echo $ROOT

docker build . -f deploy/Dockerfile.api -t biorand-re4r:main
