#build stage
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

RUN apt-get update \
    && apt-get -y install nodejs npm \
    && npm i npm@latest -g \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /source

COPY src/*.sln .
COPY src/SignalRadio.Public.Lib/*.csproj SignalRadio.Public.Lib/
COPY src/SignalRadio.Web.Client/*.csproj SignalRadio.Web.Client/
COPY src/SignalRadio.Web.Client.Test/*.csproj SignalRadio.Web.Client.Test/
COPY src/SignalRadio.LiquidBridge/*.csproj SignalRadio.LiquidBridge/
COPY src/SignalRadio.Database.EF/*.csproj SignalRadio.Database.EF/
COPY src/SignalRadio.Web.Api/*.csproj SignalRadio.Web.Api/
COPY src/SignalRadio.Web.UI/*.csproj SignalRadio.Web.UI/

RUN dotnet restore SignalRadio.sln

COPY src/ .
RUN dotnet build -c Release --no-restore

###Publish Stage
FROM build AS publish
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.Public.Lib SignalRadio.Public.Lib/*.csproj
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.Web.Client SignalRadio.Web.Client/*.csproj
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.LiquidBridge SignalRadio.LiquidBridge/*.csproj
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.Database.EF SignalRadio.Database.EF/*.csproj
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.Web.UI SignalRadio.Web.UI/*.csproj
RUN dotnet publish -c Release --no-build -o /app/SignalRadio/SignalRadio.Web.Api SignalRadio.Web.Api/*.csproj

###API Service Image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as api

WORKDIR /app

COPY --from=publish /app/SignalRadio/SignalRadio.Web.Api .
EXPOSE 443
ENTRYPOINT ["dotnet", "SignalRadio.Web.Api.dll"]

###UI Service Image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as ui

WORKDIR /app

COPY --from=publish /app/SignalRadio/SignalRadio.Web.UI .
EXPOSE 443
ENTRYPOINT ["dotnet", "SignalRadio.Web.UI.dll"]

##Icecast2 Image
FROM debian:stable-slim AS icecast

ENV DEBIAN_FRONTEND=noninteractive

ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8

ARG USERNAME=sigrad
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN apt-get -qq -y update \
    && apt-get -qq -y full-upgrade \ 
    && apt-get -qq -y install sudo icecast2 python-setuptools sudo cron-apt \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

COPY .dockerfiles/docker-sr/icecast/app /etc/icecast2
COPY .dockerfiles/docker-sr/icecast/icecast.sh /app/icecast.sh

RUN chown -R icecast2 /etc/icecast2; \
    sed -i 's/ -d//' /etc/cron-apt/action.d/3-download 

EXPOSE 8000

WORKDIR /app
CMD ["/app/icecast.sh"]

###Trunk Recorder Image
FROM robotastic/trunk-recorder:latest AS trunk-recorder

ENV DEBIAN_FRONTEND=noninteractive

ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8

ARG USERNAME=liquidsoap
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN apt-get update \
    && apt-get -y install apt-transport-https wget sudo \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get -y install dotnet-runtime-3.1 lame liquidsoap \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir /var/run/liquidsoap

COPY .dockerfiles/docker-sr/trunk-recorder /app/trunk-recorder
COPY .dockerfiles/docker-sr/liquid-bridge /app/liquid-bridge
COPY --from=publish /app/SignalRadio/SignalRadio.LiquidBridge /app/liquid-bridge/app

RUN chown -R liquidsoap:root /app
RUN chown -R liquidsoap:root /var/run/liquidsoap

WORKDIR /app
CMD ["/bin/bash", "-c", "sudo -Eu liquidsoap /app/liquid-bridge/app/SignalRadio.LiquidBridge config:/app/liquid-bridge/config/config.json server && /app/recorder --config=/app/trunk-recorder/config/config.json"]
#CMD ["/app/recorder", "--config=/app/trunk-recorder/config/config.json"]