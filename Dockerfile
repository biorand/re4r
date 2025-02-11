FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /w
COPY . /w
RUN dotnet publish src/biorand-re4r -c release -o /out -p:PublishSingleFile=true

FROM alpine
RUN apk add --no-cache libstdc++
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
COPY --from=build /out/biorand-re4r /usr/bin/biorand-re4r
RUN biorand-re4r --version

CMD /usr/bin/biorand-re4r
