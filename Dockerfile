FROM ubuntu:22.04
RUN apt-get update && apt-get install -y libicu-dev curl p7zip-full

RUN cd /

RUN curl -OL https://gitee.com/raphaelcheung/zircon-legend-server/releases/download/v0.2.1/server-v0.2.1-linux-x64.zip

RUN 7z x server-v0.2.1-linux-x64.zip -o/zircon
RUN chmod -R 777 /zircon

WORKDIR /zircon

CMD ["/zircon/Server"]