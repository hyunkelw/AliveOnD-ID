#!/usr/bin/env sh

HERE="$(dirname "$(readlink -f "$0")")"

if [ -f "$HERE/.env" ]; then
    set -a
    . "$HERE/.env"
    set +a
fi

docker build -t alive:dev  -f Containerfile  . || exit 1
docker stop alive > /dev/null 2>&1 || true
docker rm alive > /dev/null 2>&1 || true
docker run --rm --init -d --name alive \
    -p 5000:5000 \
    -e ASPNETCORE_URLS=http://0.0.0.0:5000 \
    -e "EVE_API_KEY=${EVE_API_KEY}" \
    -e "DID_API_KEY=${DID_API_KEY}" \
    -e "AZURE_SPEECH_API_KEY=${AZURE_SPEECH_API_KEY}" \
    alive:dev
