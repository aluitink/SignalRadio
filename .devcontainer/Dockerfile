FROM mcr.microsoft.com/dotnet/core/sdk:3.1  AS base

ARG USERNAME=radio
ARG USER_UID=1000
ARG USER_GID=$USER_UID

WORKDIR /app

# Create the user
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME 
    
RUN apt-get update \
    && apt-get -y install sudo nodejs npm liquidsoap \
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
#USER $USERNAME