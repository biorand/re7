FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /w
COPY . /w
RUN dotnet publish src/biorand-re7/biorand-re7.csproj -c Release -o /out -p:PublishSingleFile=true

FROM alpine
RUN apk add --no-cache libstdc++
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
COPY --from=build /out/biorand-re7 /usr/bin/biorand-re7
RUN biorand-re7 --version

CMD /usr/bin/biorand-re7
