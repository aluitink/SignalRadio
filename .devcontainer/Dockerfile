FROM robotastic/trunk-recorder:latest  AS base

ARG USERNAME=radio
ARG USER_UID=1000
ARG USER_GID=$USER_UID

WORKDIR /app

# Create the user
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID -m $USERNAME 

RUN apt-get update \
    && apt-get -y install \
        apt-utils \
        apt-transport-https \
        wget \
        sudo \
    && wget -q https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install \
        dotnet-sdk-3.1 \
        lame \
        liquidsoap \
        sqlite3 \
        icecast2 \
        python-setuptools \
        cron-apt \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME \
    #&& npm i npm@latest -g \
    #
    # Clean up
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

ENV DEBIAN_FRONTEND=dialog