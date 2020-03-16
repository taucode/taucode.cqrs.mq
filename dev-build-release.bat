dotnet restore

dotnet build --configuration Debug
dotnet build --configuration Release

dotnet test -c Debug .\tests\TauCode.Cqrs.Mq.Tests\TauCode.Cqrs.Mq.Tests.csproj
dotnet test -c Release .\tests\TauCode.Cqrs.Mq.Tests\TauCode.Cqrs.Mq.Tests.csproj

nuget pack nuget\TauCode.Cqrs.Mq.nuspec
