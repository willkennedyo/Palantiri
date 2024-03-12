# Palantiri
 Instrumentalização de aplicações .NET usando opentelemetry + AWS SQS,S3 e SNS

## Como configurar as variáveis de ambiente

Crie um arquivo .env na pasta ./src/ substituindo os campos nulos com as configurações de acordo com o seu ambiente:


```
ASPNETCORE_Kestrel__Certificates__Default__Password=
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
AWS_ACCESSKEY=
AWS_SECRETKEY=
AWS_SERVICEURL=
AWS_CONSUME=
AWS_TIMEOUT=
AWS_QUEUE=
AWS_S3_URL=
AWS_S3=
AWS_GLACIER_URL=
AWS_GLACIER=
```

## Como iniciar um contêiner com suporte a https usando o Docker Compose
Use as instruções a seguir para a configuração do seu sistema operacional.

### Windows usando contêineres do Linux
Para gerar um certificado e configurar um computador local:

```
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\aspnetapp.pfx"  -p $CREDENTIAL_PLACEHOLDER$
dotnet dev-certs https --trust
```

O comando anterior usando a CLI do .NET:


```
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p $CREDENTIAL_PLACEHOLDER$
dotnet dev-certs https --trust
```
Nos comandos anteriores, substitua `$CREDENTIAL_PLACEHOLDER$` por uma senha.

Crie um arquivo docker-compose.debug.yml com o seguinte conteúdo:

```
version: '3.4'

services:
  webapp:
    image: mcr.microsoft.com/dotnet/samples:aspnetapp
    ports:
      - 80
      - 443
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=password
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    volumes:
      - ~/.aspnet/https:/https:ro
```
A senha especificada no arquivo do Docker Compose precisa corresponder à senha usada para o certificado.

Inicie o contêiner com o ASP.NET Core configurado para HTTPS:


```
docker-compose -f "docker-compose.debug.yml" up -d
```

### macOS ou Linux
Para gerar um certificado e configurar um computador local:

CLI do .NET

```
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p $CREDENTIAL_PLACEHOLDER$
dotnet dev-certs https --trust
```
O dotnet dev-certs https --trust só tem suporte no macOS e no Windows. No Linux você precisa confiar em certificados de uma maneira que seja compatível com a sua distribuição. É provável que você precise confiar no certificado do seu navegador.

Nos comandos anteriores, substitua `$CREDENTIAL_PLACEHOLDER$` por uma senha.

Crie um arquivo docker-compose.debug.yml com o seguinte conteúdo:

```
version: '3.4'

services:
  webapp:
    image: mcr.microsoft.com/dotnet/samples:aspnetapp
    ports:
      - 80
      - 443
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=password
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    volumes:
      - ~/.aspnet/https:/https:ro
```
A senha especificada no arquivo do Docker Compose precisa corresponder à senha usada para o certificado.

Inicie o contêiner com o ASP.NET Core configurado para HTTPS:

```
docker-compose -f "docker-compose.debug.yml" up -d
```


