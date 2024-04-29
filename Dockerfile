FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-api
WORKDIR /w
COPY . /w
RUN dotnet publish src/biorand-re4r -c release -o /out -p:PublishSingleFile=true

FROM node:22-alpine AS build-web
WORKDIR /w
COPY . /w
RUN cd src/biorand-re4r-web \
 && npm i \
 && npm run build

FROM alpine
RUN apk add --no-cache libstdc++ nginx sqlite-libs \
 && ln -s libsqlite3.so.0 /usr/lib/libsqlite3.so
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
COPY --from=build-api /out/biorand-re4r /usr/bin/biorand-re4r
COPY --from=build-web /w/src/biorand-re4r-web/build /usr/share/biorand-re4r/www
RUN biorand-re4r --version

RUN CFG=/etc/nginx/http.d/default.conf \
 && echo 'server {' > $CFG \
 && echo '    listen 80 default_server;' >> $CFG \
 && echo '    listen [::]:80 default_server;' >> $CFG \
 && echo '    location / {' >> $CFG \
 && echo '        root    /usr/share/biorand-re4r/www;' >> $CFG \
 && echo '        index   index.html;' >> $CFG \
 && echo '    }' >> $CFG \
 && echo '}' >> $CFG

RUN echo '#!/bin/sh' > /usr/bin/run-api \
 && echo 'biorand-re4r web-server >> /root/.biorand/api.log' >> /usr/bin/run-api \
 && chmod +x /usr/bin/run-api

RUN echo '#!/bin/sh' > /usr/bin/run-www \
 && echo 'nginx && tail -f /var/log/nginx/access.log /var/log/nginx/error.log' >> /usr/bin/run-www \
 && chmod +x /usr/bin/run-www

EXPOSE 80/tcp
