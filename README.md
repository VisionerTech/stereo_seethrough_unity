
## stereo_seethrough_unity

this is the stereo see through project for VisionerTech VMG-PROV 01 which contains visual studio part and unity part. It can be used to see the surroundings as naked eyes.

## Requirement:

1.  recommended specs: Intel Core i5-4460/8G RAM/GTX 660/at least two USB3.0/
2.  windows x64 version.(tested on win7/win10)

## How to Run
1. copy the parameters from stereo calibration project or stereo calibration excutable project to "/save_param/" here.
2. open a command window and run stere_seethrough.exe with corresponding Camera Index. First parameter is the left eye camera index, and the second is the right.
>>stereo_seethrough.exe 0 1

  put your hand in front of the left camera. Check whether the left image changes. If not, the system index of the left camera is 1 instead of 0.  So we should run with the parameter list as below:
>>stereo_seethrough.exe 1 0
