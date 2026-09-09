FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /zircon

ARG REPO=raphaelcheung/zircon-legend-server
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl jq unzip \
 && rm -rf /var/lib/apt/lists/*

# 自动拉取最新 release 的 linux-x64 包并解压
RUN ASSET_URL=$(curl -fsSL "https://api.github.com/repos/${REPO}/releases/latest" \
      | jq -r '[.assets[] | select(.name | endswith("linux-x64.zip")) | .browser_download_url][0]') \
 && if [ -z "$ASSET_URL" ] || [ "$ASSET_URL" = "null" ]; then echo "未找到 linux-x64.zip 资产" >&2; exit 1; fi \
 && echo "下载: $ASSET_URL" \
 && curl -fsSL "$ASSET_URL" -o /tmp/pkg.zip \
 && unzip -q /tmp/pkg.zip -d /zircon \
 && chmod +x /zircon/Server \
 && rm /tmp/pkg.zip

RUN mkdir -p datas Map
EXPOSE 7000 3000 7080
ENTRYPOINT ["./Server"]
