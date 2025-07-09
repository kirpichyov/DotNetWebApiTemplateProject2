FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY src .
COPY ./entrypoint.sh /app/entrypoint.sh

RUN dotnet restore "./SampleProject.Api/SampleProject.Api.csproj"
RUN dotnet build "./SampleProject.Api/SampleProject.Api.csproj" -c Release --no-restore

ENV PATH="${PATH}:~/.dotnet/tools"
RUN dotnet tool install -g dotnet-ef  --version 9.0.4
RUN dotnet ef migrations bundle -v --force --startup-project './SampleProject.Api/' --project './SampleProject.DataAccess/' --output /app/efbundle

CMD ["chmod", "+x", "SampleProject.Api"]
RUN dotnet publish "./SampleProject.Api/SampleProject.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /app/efbundle /app/efbundle
COPY --from=build /app/entrypoint.sh /app/entrypoint.sh
RUN chmod +x ./entrypoint.sh ./efbundle

ENTRYPOINT ["/app/entrypoint.sh"]