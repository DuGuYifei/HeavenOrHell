package main

import (
	"bufio"
	"bytes"
	"github.com/gin-gonic/gin"
	"io"
	"net/http"
	"os/exec"
	"runtime"
	"sync"
)

var serverRunning = false
var serverCmd *exec.Cmd
var outputBuffer = NewRingBuffer(100)
var bufferLock sync.Mutex

type RingBuffer struct {
	lines []string
	size  int
	pos   int
	full  bool
}

func NewRingBuffer(size int) *RingBuffer {
	return &RingBuffer{
		lines: make([]string, size),
		size:  size,
	}
}

func (rb *RingBuffer) Add(line string) {
	rb.lines[rb.pos] = line
	rb.pos = (rb.pos + 1) % rb.size
	if rb.pos == 0 {
		rb.full = true
	}
}

func (rb *RingBuffer) GetAll() []string {
	if rb.full {
		res := make([]string, rb.size)
		copy(res, rb.lines[rb.pos:])
		copy(res[rb.size-rb.pos:], rb.lines[:rb.pos])
		return res
	}
	return rb.lines[:rb.pos]
}

func main() {
	r := gin.Default()

	r.GET("/", func(c *gin.Context) {
		c.Header("Content-Type", "text/html")
		c.String(http.StatusOK, `
            <html>
            <head>
                <title>Server Toggle</title>
                <script>
                    let logIntervalId = null; // Keep a global variable for the log interval timer

                    async function fetchLog() {
                        const response = await fetch('/log');
                        const text = await response.text();
                        document.getElementById('log').textContent = text;
                    }

                    async function getStatus() {
                        const response = await fetch('/status');
                        const data = await response.json();
                        document.getElementById('toggleButton').textContent = data.buttonText;

                        // Clear any existing log polling
                        if (logIntervalId) {
                            clearInterval(logIntervalId);
                            logIntervalId = null;
                        }

                        // If server is running, start polling logs
                        if (data.isRunning) {
                            fetchLog(); // Fetch logs immediately
                            logIntervalId = setInterval(fetchLog, 2000);
                        } else {
                            // Clear log display if server is not running
                            document.getElementById('log').textContent = '';
                        }
                    }

                    async function toggleServer() {
                        document.getElementById('toggleButton').disabled = true;
                        await fetch('/toggle', { method: 'POST' }); // Make the request
                        document.getElementById('toggleButton').disabled = false;
                        getStatus(); // Update status and manage log polling
                    }

                    window.onload = function() {
                        getStatus(); // Fetch initial status and set up log polling if needed
                    };
                </script>
            </head>
            <body>
                <button type="button" id="toggleButton" onclick="toggleServer()">Loading...</button>
                <pre id="log"></pre>
            </body>
            </html>
        `)
	})

	r.POST("/toggle", func(c *gin.Context) {
		if runtime.GOOS != "linux" {
			c.JSON(http.StatusOK, gin.H{
				"success":    false,
				"buttonText": "非Linux环境，仅供开发测试",
			})
			return
		}
		bufferLock.Lock()
		defer bufferLock.Unlock()

		if !serverRunning {
			// Wrap the target executable with `stdbuf -oL -eL` so that stdout/stderr become line-buffered
			// This ensures we can read C++ program output in real time
			serverCmd = exec.Command("stdbuf", "-oL", "-eL", "/opt/server/hoh/HeavenOrHellServer")

			stdout, errPipeOut := serverCmd.StdoutPipe()
			if errPipeOut != nil {
				outputBuffer.Add("[error] Failed to create stdout pipe: " + errPipeOut.Error())
				c.JSON(http.StatusInternalServerError, gin.H{
					"success":    false,
					"buttonText": "启动失败",
					"error":      "Failed to create stdout pipe: " + errPipeOut.Error(),
				})
				return
			}

			stderr, errPipeErr := serverCmd.StderrPipe()
			if errPipeErr != nil {
				outputBuffer.Add("[error] Failed to create stderr pipe: " + errPipeErr.Error())
				c.JSON(http.StatusInternalServerError, gin.H{
					"success":    false,
					"buttonText": "启动失败",
					"error":      "Failed to create stderr pipe: " + errPipeErr.Error(),
				})
				return
			}

			err := serverCmd.Start()
			if err == nil {
				serverRunning = true
				outputBuffer = NewRingBuffer(100)
				go func() {
					scanPipe := func(pipe io.Reader, prefix string) {
						scanner := bufio.NewScanner(pipe)
						for scanner.Scan() {
							bufferLock.Lock()
							outputBuffer.Add(prefix + scanner.Text())
							bufferLock.Unlock()
						}
						if err := scanner.Err(); err != nil {
							bufferLock.Lock()
							outputBuffer.Add(prefix + "[scanner_error] " + err.Error())
							bufferLock.Unlock()
						}
					}
					var wg sync.WaitGroup
					wg.Add(2)
					go func() { defer wg.Done(); scanPipe(stdout, "[stdout] ") }()
					go func() { defer wg.Done(); scanPipe(stderr, "[stderr] ") }()
					wg.Wait()
					bufferLock.Lock()
					serverRunning = false
					serverCmd = nil
					bufferLock.Unlock()
				}()
				go func() {
					err := serverCmd.Wait()
					bufferLock.Lock()
					serverRunning = false
					serverCmd = nil
					if err != nil {
						outputBuffer.Add("[process_exit] " + err.Error())
					}
					bufferLock.Unlock()
				}()
				c.JSON(http.StatusOK, gin.H{
					"success":    true,
					"buttonText": "关闭服务器",
				})
			} else {
				c.JSON(http.StatusInternalServerError, gin.H{
					"success":    false,
					"buttonText": "启动失败",
					"error":      err.Error(),
				})
			}
		} else {
			if serverCmd != nil && serverCmd.Process != nil {
				err := serverCmd.Process.Kill()
				if err != nil {
					exec.Command("pkill", "-f", "HeavenOrHellServer").Run()
				}
				// 新增：Kill 后调用 Wait()，防止僵尸进程
				go func(cmd *exec.Cmd) {
					cmd.Wait()
				}(serverCmd)
			} else {
				exec.Command("pkill", "-f", "HeavenOrHellServer").Run()
			}
			serverRunning = false
			serverCmd = nil
			c.JSON(http.StatusOK, gin.H{
				"success":    true,
				"buttonText": "启动服务器",
			})
		}
	})

	r.GET("/log", func(c *gin.Context) {
		bufferLock.Lock()
		lines := outputBuffer.GetAll()
		bufferLock.Unlock()
		var buf bytes.Buffer
		for _, l := range lines {
			buf.WriteString(l)
			buf.WriteByte('\n')
		}
		c.String(http.StatusOK, buf.String())
	})

	r.GET("/status", func(c *gin.Context) {
		bufferLock.Lock()
		defer bufferLock.Unlock()
		buttonText := "启动服务器"
		if serverRunning {
			buttonText = "关闭服务器"
		}
		c.JSON(http.StatusOK, gin.H{
			"isRunning":  serverRunning,
			"buttonText": buttonText,
		})
	})

	r.Run(":8889")
}
