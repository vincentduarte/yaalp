To debug this, at least for Vs2005c# express, you must disable an exception.

If, when you try to run this, it says
Message: DLL '.......\Microsoft.DirectX.Direct3D.dll' is attempting 
managed execution inside OS Loader lock. Do not attempt to run 
managed code inside a DllMain or image initialization function since
doing so can cause the application to hang.


If this happens, Disable the loader lock MDA. In vs, Debug menu -> Exceptions (ctrl-D, E), Open the Managed Debugging Assistants tree node and uncheck Loader Lock. This setting is per solution so it will only affect this solution.