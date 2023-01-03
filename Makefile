PreviewXamlFile := Views/Windows/Main/MainWindow.axaml

gen-code:
	bash ./eng/gen-code.sh

test:
	dotnet test --no-restore --filter "FullyQualifiedName~Omnius.Lxna"

build:
	dotnet build

run-designer: build
	dotnet msbuild ./src/Omnius.Lxna.Ui.Desktop/ /t:Preview /p:XamlFile=$(PreviewXamlFile)

update-dotnet-tool:
	bash ./eng/update-dotnet-tool.sh

update-sln:
	bash ./eng/update-sln.sh

clean:
	rm -rf ./bin
	rm -rf ./tmp
	rm -rf ./pub

.PHONY: test build
