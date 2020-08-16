FROM robotastic/gnuradio:latest

ARG USERNAME=radio
ARG USER_UID=1000
ARG USER_GID=$USER_UID

RUN apt-get update \
    && apt-get -y install sudo liquidsoap \
    && echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME \
    && chmod 0440 /etc/sudoers.d/$USERNAME

WORKDIR /src

COPY . .

RUN cmake . && make && cp recorder /recorder

RUN mkdir -p /app/trunk-recorder
RUN mkdir -p /app/trunk-recorder/config
RUN mkdir -p /app/trunk-recorder/data

ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8

WORKDIR /

USER nobody
CMD ["./recorder","--config=/app/trunk-recorder/config/config.json"]