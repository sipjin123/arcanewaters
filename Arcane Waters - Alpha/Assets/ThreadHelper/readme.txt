How do I use it?
----------------

You can use inside your scripts the UnityThreadingHelper class.
This class provides a bunch of static methods and properties:

UnityThreadingHelper.Dispatcher - tell the MainThread to run a bunch of commands (Unity commands for example)
UnityThreadingHelper.TaskDistributor - run a bunch of commands in a free worker thread
UnityThreadingHelper.CreateThread - create a new thread which runs the given bunch of commands

How do I remove it?
-------------------
The UnityThreadingHelper will automatically remove every thread when leaving the game mode.
But if you want manual control over the life time of the created Threads and Tasks you can call Dispose()
on almost every class in this package.

How do I create a new thread?
-----------------------------
To create a new Thread call one of the 4 versions of UnityThreadHelper.CreateThread:

2 simple examples:
Code:  
UnityThreading.ActionThread myThread;

void Start()
{
  myThread = UnityThreadHelper.CreateThread(DoThreadWork);
}

void DoThreadWork()
{
  // processing
}

Code:  
UnityThreading.ActionThread myThread; 

void Start()
{
  myThread = UnityThreadHelper.CreateThread(() =>
  {
    // processing
  });
}

Which version you prefer is up to you 
You dont even need to store the return value of CreateThread, the UnityThreadHelper will take care of finished threads.
But if you want to maybe send an abort signal to the thread processing method you will need to store it.

How do I check inside the thread if the thread should abort, and how do I abort it?
-----------------------------------------------------------------------------------

Easy, simply check in your thread function if the ShouldStop property is set to true:

Code:  
void DoThreadWork(UnityThreading.ActionThread thread)
{
  // processing
  
  // check if we should stop
  if (thread.ShouldStop)
    return; // finish

  // more processing when we should not stop yet
}

Or:
Code:  
void DoThreadWork()
{
  // processing
 
 // check if we should stop
  if (UnityThreading.ActionThread.CurrentThread.ShouldStop)
    return; // finish

  // more processing when we should not stop yet
}

And this is how you can signal the thread to stop:

Code:  
myThread.Exit(); // this signals the thread to stop
myThread.Abort(); // this forces the thread to stop and waits until the thread has stopped.
myThread.AbortWaitForSeconds(10.0f); // this forces the thread to stop and waits max. 10 seconds, when its not stopped after this time, the thread will be terminated

A thread will automatically stop when the thread process function leaves.

How do I call unity commands and methods with it?
When you need to call something at the MainThread you can make use of the Dispatcher,
the Dispatcher is a little class which will be updated each frame and will process all commands
it has been send from an other thread.
The most easy way to send a command or a bunch of commands into the MainThread is this:

Code:  
void DoThreadWork()
{
  // instantiate the asset "enemy"
  UnityThreadHelper.Dispatcher.Dispatch(() => Instantiate(Resources.Load("enemy")));

  // or like this:
  var objectToInstantiate = "enemy";
  UnityThreadHelper.Dispatcher.Dispatch(() => Instantiate(Resources.Load(objectToInstantiate)));

  // or more like this:
  var health = Mathf.Random(10, 100);
  UnityThreadHelper.Dispatcher.Dispatch(() =>
  {
    var newGameObject = Instantiate(Resources.Load(objectToInstantiate));

    newGameObject.GetComponent<MyComponent>().health = health;
  });
}

The Dispatch method returns an instance of the Task class, if you want you can use
it for example to wait for completion of the dispatched operation:

Code:  
var task = UnityThreadHelper.Dispatcher.Dispatch(...);
task.Wait();

Also a great feature of the Dispatcher is the ability to dispatch not only methods, but also functions:

Code:  
var task = UnityThreadHelper.Dispatcher.Dispatch(() => { return 1+1; });
var result = task.Wait<int>();

Code:  
var task = UnityThreadHelper.Dispatcher.Dispatch(() => { return 1+1; });
task.Wait();

var result  = task.Result;

There are many many more features to discover, just throw a look into what IntelliSense displays for you or ask me 

How do I create simple background Tasks and why not use a extra thread?
-----------------------------------------------------------------------

If you have many sub operations you dont want to create an extra thread,
every thread created will lower the overall performance of the process and will
also take some time to be created (noticeable when you create many threads).

Simple background tasks dispatched by the TaskDistributor class will allow a very large
amount of tasks to be processed without the need of the creation of many extra threads.

To use the TaskDistributor just call UnityThreadHelper.TaskDistributor and use the instance like a Dispatcher:

Code:  
UnityThreadHelper.TaskDistributor.Dispatch(...);

It works exactly like the Dispatcher with the difference that every task will be processed in the background.
The current implementation of the TaskDistributor allows up to ProcessorCount multiplied by 3 parallel tasks to be operated.
Dont worry, if you dispatch more than this amount, the tasks will be processed at the time the amount is below this value.

Reducing Size:
--------------

Delete the UnityThreadHelper.ThreadSafe.dll when not using it will reduce the ammount space needed by nearly 1.7MB.

Further Links:
--------------

API Reference:
http://dras.biz/download/UnityThreadHelper/Help/Index.html

Initial Unity Forum thread:
http://forum.unity3d.com/threads/90128-Unity-Threading-Helper
