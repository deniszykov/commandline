SET OUTPUT_DIR=%1
IF "%OUTPUT_DIR%"=="" SET OUTPUT_DIR=%CD%
nuget pack ConsoleApp.CommandLine.csproj -Prop Configuration=Release -Prop Platform=AnyCPU -OutputDirectory %OUTPUT_DIR%