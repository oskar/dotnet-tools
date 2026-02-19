dotnet pack -o out
dotnet tool uninstall -g dotnet-overview
dotnet tool uninstall -g dotnet-open
dotnet tool install -g dotnet-overview --add-source out --no-cache
dotnet tool install -g dotnet-open --add-source out --no-cache
