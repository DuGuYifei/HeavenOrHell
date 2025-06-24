:: 交叉编译为 Linux
set GOOS=linux
set GOARCH=amd64
go build -o hoh_controller

:: 恢复为 Windows
set GOOS=windows
set GOARCH=amd64