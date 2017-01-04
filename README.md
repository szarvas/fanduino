# fanduino
Ardunio Nano based PC fan controller software. Windows only. You can download the prebuilt Windows binaries from the [release](https://github.com/szarvas/fanduino/ releases) menu.

![Fanduino Hardware Schematic](/documentation/fanduino-hw.png)

This repository contains 3 projects.

### fanduino-nano

This contains the code you have to upload to your Arduino Nano. Once done, your Arduino will continuously report fan speeds and duty cycles on the serial port. It will also accept two string commands beginning with `c 1` and `c 2`. Note that commands must be terminated by either a space or a newline.

`c 1 40 50 40 30 ` Sending this command will instruct the Arduino to drive fan0 at 40%, fan1 at 50% etc. for the next roughly 3 seconds. This is the command that the PC software is sending to adjust fan speeds according to CPU and GPU temperatures.

`c 2 60 60 60 60 ` Sending this command will instruct the Arduino to set all fan speeds to 60% when not instructed otherwise by `c 1`. In other words this is the fallback fan speed that Arduino uses whenever there is no communication with the PC software. These speeds should be set as failsafe values, so they should be high enough to adequately cool your system even under load.


### Fanduino

This is a standalone PC software containing all of the control logic. You can configure it through the `Config.yaml` file, which must be placed in the same directory as the main executable. It runs in a console and may not be convenient for long term use.


### FanduinoWindow

This is a minimal program with a graphical user interface and is nothing but a thin wrapper around the Fanduino project. It depends on the Fanduino executable, so before building this, you have to build that first. Run the FanduinoWindow.exe to launch it. It requires the same `Config.yaml` file. Unlike the console version it can be conveniently minimized to system tray. You can set up the Windows Task Scheduler to launch it at user log on.

![Fanduino Hardware Schematic](/documentation/gui.png)
