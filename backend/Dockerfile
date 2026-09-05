# Builds the deployable Lambda artifact: a Native AOT binary named `bootstrap`, zipped, for the
# provided.al2023 runtime.
#
# Docker isn't a convenience here - Native AOT emits machine code for the platform it runs on and
# can't cross-compile, so the binary has to be produced on Amazon Linux 2023 to match the glibc
# Lambda provides. Build it with:
#
#     docker build -f backend/Dockerfile --target artifact \
#       --output type=local,dest=backend/publish backend
#
# which leaves backend/publish/function.zip ready for `sam deploy` or
# `aws lambda update-function-code`. See backend/template.yaml.
#
# For running the API locally, use Dockerfile.local instead - it's a normal build and much faster.
FROM public.ecr.aws/amazonlinux/amazonlinux:2023 AS build

# clang is the native toolchain the AOT compiler links through; zlib and openssl get linked into
# the binary itself. libicu is only for the .NET SDK's own use while building - the app publishes
# with InvariantGlobalization, so no ICU dependency reaches the artifact (or Lambda, which has none).
RUN dnf install -y clang zlib-devel openssl-devel krb5-libs libicu tar gzip zip findutils \
    && dnf clean all

ARG DOTNET_CHANNEL=10.0
RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel ${DOTNET_CHANNEL} --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh
ENV PATH="/usr/share/dotnet:${PATH}" \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# linux-x64 by default so this builds natively on an x64 host. Pass linux-arm64 to target
# Graviton (cheaper per ms), but note that needs an arm64 builder or emulation, and the
# Architectures in template.yaml has to match.
ARG RUNTIME_ID=linux-x64

WORKDIR /src

COPY src/PlumeriaStore.Api/PlumeriaStore.Api.csproj src/PlumeriaStore.Api/
RUN dotnet restore src/PlumeriaStore.Api/PlumeriaStore.Api.csproj -r ${RUNTIME_ID}

COPY src/PlumeriaStore.Api/ src/PlumeriaStore.Api/
RUN dotnet publish src/PlumeriaStore.Api/PlumeriaStore.Api.csproj \
    -c Release -r ${RUNTIME_ID} --no-restore -o /publish

# provided.al2023 runs whatever executable is named `bootstrap`, and needs it marked executable
# inside the archive - which is why the zip is built here rather than on the host, where a
# Windows filesystem would drop the mode bit.
#
# appsettings.json ships alongside it (the function reads its defaults from there, with the stack's
# environment variables layered on top). appsettings.Development.json does not: it points at the
# local emulator containers, and is no use - or actively wrong - anywhere else. The .dbg file is
# separated-out debug symbols, ~58MB of them, and Lambda has no use for it either.
RUN mv /publish/PlumeriaStore.Api /publish/bootstrap \
    && chmod +x /publish/bootstrap \
    && cd /publish \
    && zip -q -j function.zip bootstrap appsettings.json

# Nothing but the archive, so `--output type=local` writes exactly one file to the host.
FROM scratch AS artifact
COPY --from=build /publish/function.zip /function.zip
