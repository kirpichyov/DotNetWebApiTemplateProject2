#!/usr/bin/env bash
set -euo pipefail

# 1) Run your self-contained migrations bundle
exec /app/efbundle "$@" & wait $!

# 2) Start your ASP-NET app via the .NET host
exec dotnet /app/SampleProject.Api.dll
