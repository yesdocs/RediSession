RedisLibrary.RediSession
------------------------

RediSession is an ASP.NET Session Module Handler that maps Session to a REDIS store

The Redislibrary is a collection of classes that allow you to swap out your current
ASP.NET Session State with a multi-threaded non-blocking Session Provider that is
backed by a NoSQL store: REDIS.

<br />

It just works! This library is an attempt to make the transition to Non-Blocking
Session State simple, transparent and easy. To achieve this: 

* These classes are simple to understand, not a lot of magic going on.
* These classes are fast to use and require little to no training.
* On the average, I have seen a 20% increase in web site performance when using this library (results may vary).
 

<br />

### Getting Started

Getting started is rather easy. No code is replaced and you can continue to use
Session as you do today, so there is no new paradigms to learn.

* First you must include this library in your references. This can be achieved by referencing the project in your solution (Visual Studio) or including the dll: 'RediSessionLibrary.dll' in your bin folder.
* Next you need to setup your connection to point to your instance of REDIS in your web.config for Session State.
* And finally, setup the Session module handler in your web.config to use the included 'RediSessionLibrary.RedisSessionStateModule'

#### SessionState changes in web.config

The following node must have these elements: 
  
```xml
<configuration>
	<system.web>
		<sessionState
			 stateConnectionString="tcp=localhost:6379" <!-- This is your connection port to REDIS -->
			 mode="Custom" 
			 customProvider="RedisSession"
			 cookieName="RediSessionID" > 
			<providers>
			</providers>
		</sessionState>
	</system.web>
</configuration>
```

#### Session module handler changes in your web.config

For Integrated IIS Website App Pools, include the following in your web.config:

```xml
<configuration>
	<system.webServer>
		<modules>
			<remove name="Session" />
			<add name="Session" type="RediSessionLibrary.RedisSessionStateModule" />
		</modules>
	</system.webServer>
</configuration>
```

For Non Integrated IIS Website App Pools, use the following nodes instead in your web.config:

```xml
<configuration>
	<system.web>
		<httpModules>
			<remove name="Session" />
			<add name="Session" type="RediSessionLibrary.RedisSessionStateModule" />
		</httpModules>
	</system.web>
</configuration>
```
<br />

### How To Use

A couple of standard practices apply as would in every scenario:
* Try to minimize Session usage if at all possible. Sounds weird? Well, for years we in the .NET world have tried to go completly stateless becasue of the Sesion State scalability and blocking attributes that ASP.NET Session state offered. So, now that it's not that harmful to use Session, do't start using it all willy-nilly like, only when you need to.
* Try to use value types. Complex objects are OK, but be aware of racing conditions in the browser where one thread changes one attribute and another changes yet another attibute OF THE SAME OBJECT. In that case, the last thread wins with that object's state.

You can continue to use session as you wold normally do.

#### Value types:

```csharp
	// at the begining of the request a GetAsync() was issued for all Session items. 

	// The first Session[] call will wait for the GetAsync() to finish before returning a value
	string stuff_about_the_user = Session["stuff"];
	  
	...
	
	Session["stuff"] = data.ToString() ;	// this will Fire a SetAsync() to to set the state of this key
```

#### Complex Objects:

```csharp
	// at the begining of the request a GetAsync() was issued for all Session items. 

	// The first Session[] call will wait for the GetAsync() to finish before returning a value

	SomeFunkyThing thingy = Session["that_thing_about_you"];
	
	thingy.FooBar.bFoo = true;						// nothing set (yet) in the REDIS Server

	Session["that_thing_about_you"] = newthingy ;	// this will Fire a SetAsync() to to set the state of this key

	thingy.FooBar.bBar = false;						// again, nothing set (yet) in the REDIS Server
	 
	// serialized version of "that_thing_about_you" after the last SetAsync() is compare to the current state, if it is different, it is sent again via SetAsync()

	// at the end of the request we wait for all SetAsync() calls to finish
```
  
<br />

### Contents

*  RediSessionLibrary: a csharp library that contins all the code for Session usage.
*  RedisSessionWebSample: a SAMPLE csharp website that leverages the 'RediSessionLibrary' to test the usage.

