FROM mcr.microsoft.com/dotnet/core/sdk:3.1  AS base

ARG USERNAME=radio
ARG USER_UID=1000
ARG USER_GID=$USER_UID

WORKDIR /app

# Create the user
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME 
    
RUN apt-get update \
    && apt-get -y install sudo nodejs npm \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME \
    && npm i npm@latest -g \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

ENV DEBIAN_FRONTEND=dialog

# ********************************************************
# * Anything else you want to do like clean up goes here *
# ********************************************************

# [Optional] Set the default user. Omit if you want to keep the default as root.
USER $USERNAME


#FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
# WORKDIR /src
# COPY simple.signalr/*.sln ./
# COPY simple.signalr/Simple.SignalR.Socket/Simple.SignalR.Socket.csproj Simple.SignalR.Socket/
# RUN dotnet restore Simple.SignalR.Socket/Simple.SignalR.Socket.csproj

# COPY simple.signalr/* .
# WORKDIR /src/Simple.SignalR.Socket
# RUN dotnet build Simple.SignalR.Socket.csproj -c Release -o /app

# FROM build AS publish
# RUN dotnet publish Simple.SignalR.Socket.csproj -c Release -o /app

# FROM base AS final
# WORKDIR /app

# COPY --from=publish /app .
# ENTRYPOINT ["dotnet", "Simple.SignalR.Socket.dll"]