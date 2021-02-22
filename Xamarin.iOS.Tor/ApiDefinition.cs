using System;
using ObjCRuntime;
using Foundation;

namespace Xamarin.iOS.Tor
{
	//	Binding Analysis:
	//  Automated binding is complete, but there are a few APIs which have been flagged with[Verify] attributes.While the entire binding
	//  should be audited for best API design practices, look more closely at APIs with the following Verify attribute hints:

	//  ConstantsInterfaceAssociation (1 instance):

	//	There's no foolproof way to determine with which Objective-C interface an extern variable declaration may be associated. Instances
	//    of these are bound as [Field] properties in a partial interface into a nearby concrete interface to produce a more intuitive API,
	//    possibly eliminating the 'Constants' interface altogether.

	//  PlatformInvoke(4 instances) :

	//	In general P/Invoke bindings are not as correct or complete as Objective-C bindings(at least currently). You may need to fix up the
	//   library name(it defaults to '__Internal') and return/parameter types manually to conform to C calling conventionsfor the target
	//  platform.You may find you don't even want to expose the C API in your binding, but if you do, you'll probably also want to relocate

	//  the definition to a more appropriate class and expose a stronger type-safe wrapper.For P/Invoke guidance, see http://www.mono-

	//  project.com/docs/advanced/pinvoke/.


	//Once you have verified a Verify attribute, you should remove it from the binding source code.The presence of Verify attributes
	//intentionally cause build failures.

	//For more information about the Verify attribute hints above, consult the Objective Sharpie documentation by running 'sharpie docs' or
	//visiting the following URL:

	// @interface TORNode : NSObject <NSSecureCoding>
	[BaseType(typeof(NSObject))]
	interface TORNode : INSSecureCoding
	{
		// @property (readonly, nonatomic, class) NSRegularExpression * _Nonnull ipv4Regex;
		[Static]
		[Export("ipv4Regex")]
		NSRegularExpression Ipv4Regex { get; }

		// @property (readonly, nonatomic, class) NSRegularExpression * _Nonnull ipv6Regex;
		[Static]
		[Export("ipv6Regex")]
		NSRegularExpression Ipv6Regex { get; }

		// @property (nonatomic) NSString * _Nullable fingerprint;
		[NullAllowed, Export("fingerprint")]
		string Fingerprint { get; set; }

		// @property (nonatomic) NSString * _Nullable nickName;
		[NullAllowed, Export("nickName")]
		string NickName { get; set; }

		// @property (nonatomic) NSString * _Nullable ipv4Address;
		[NullAllowed, Export("ipv4Address")]
		string Ipv4Address { get; set; }

		// @property (nonatomic) NSString * _Nullable ipv6Address;
		[NullAllowed, Export("ipv6Address")]
		string Ipv6Address { get; set; }

		// @property (nonatomic) NSString * _Nullable countryCode;
		[NullAllowed, Export("countryCode")]
		string CountryCode { get; set; }

		// @property (readonly, nonatomic) NSString * _Nullable localizedCountryName;
		[NullAllowed, Export("localizedCountryName")]
		string LocalizedCountryName { get; }

		// -(instancetype _Nonnull)initFromString:(NSString * _Nonnull)longName;
		[Export("initFromString:")]
		IntPtr Constructor(string longName);

		// -(void)acquireIpAddressesFromNsResponse:(NSString * _Nonnull)response;
		[Export("acquireIpAddressesFromNsResponse:")]
		void AcquireIpAddressesFromNsResponse(string response);
	}

	// @interface TORCircuit : NSObject <NSSecureCoding>
	[BaseType(typeof(NSObject))]
	interface TORCircuit : INSSecureCoding
	{
		// @property (readonly, class) NSRegularExpression * _Nonnull mainInfoRegex;
		[Static]
		[Export("mainInfoRegex")]
		NSRegularExpression MainInfoRegex { get; }

		// @property (readonly, class) NSString * _Nonnull statusLaunched;
		[Static]
		[Export("statusLaunched")]
		string StatusLaunched { get; }

		// @property (readonly, class) NSString * _Nonnull statusBuilt;
		[Static]
		[Export("statusBuilt")]
		string StatusBuilt { get; }

		// @property (readonly, class) NSString * _Nonnull statusGuardWait;
		[Static]
		[Export("statusGuardWait")]
		string StatusGuardWait { get; }

		// @property (readonly, class) NSString * _Nonnull statusExtended;
		[Static]
		[Export("statusExtended")]
		string StatusExtended { get; }

		// @property (readonly, class) NSString * _Nonnull statusFailed;
		[Static]
		[Export("statusFailed")]
		string StatusFailed { get; }

		// @property (readonly, class) NSString * _Nonnull statusClosed;
		[Static]
		[Export("statusClosed")]
		string StatusClosed { get; }

		// @property (readonly, class) NSString * _Nonnull buildFlagOneHopTunnel;
		[Static]
		[Export("buildFlagOneHopTunnel")]
		string BuildFlagOneHopTunnel { get; }

		// @property (readonly, class) NSString * _Nonnull buildFlagIsInternal;
		[Static]
		[Export("buildFlagIsInternal")]
		string BuildFlagIsInternal { get; }

		// @property (readonly, class) NSString * _Nonnull buildFlagNeedCapacity;
		[Static]
		[Export("buildFlagNeedCapacity")]
		string BuildFlagNeedCapacity { get; }

		// @property (readonly, class) NSString * _Nonnull buildFlagNeedUptime;
		[Static]
		[Export("buildFlagNeedUptime")]
		string BuildFlagNeedUptime { get; }

		// @property (readonly, class) NSString * _Nonnull purposeGeneral;
		[Static]
		[Export("purposeGeneral")]
		string PurposeGeneral { get; }

		// @property (readonly, class) NSString * _Nonnull purposeHsClientIntro;
		[Static]
		[Export("purposeHsClientIntro")]
		string PurposeHsClientIntro { get; }

		// @property (readonly, class) NSString * _Nonnull purposeHsClientRend;
		[Static]
		[Export("purposeHsClientRend")]
		string PurposeHsClientRend { get; }

		// @property (readonly, class) NSString * _Nonnull purposeHsServiceIntro;
		[Static]
		[Export("purposeHsServiceIntro")]
		string PurposeHsServiceIntro { get; }

		// @property (readonly, class) NSString * _Nonnull purposeHsServiceRend;
		[Static]
		[Export("purposeHsServiceRend")]
		string PurposeHsServiceRend { get; }

		// @property (readonly, class) NSString * _Nonnull purposeTesting;
		[Static]
		[Export("purposeTesting")]
		string PurposeTesting { get; }

		// @property (readonly, class) NSString * _Nonnull purposeController;
		[Static]
		[Export("purposeController")]
		string PurposeController { get; }

		// @property (readonly, class) NSString * _Nonnull purposeMeasureTimeout;
		[Static]
		[Export("purposeMeasureTimeout")]
		string PurposeMeasureTimeout { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHsciConnecting;
		[Static]
		[Export("hsStateHsciConnecting")]
		string HsStateHsciConnecting { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHsciIntroSent;
		[Static]
		[Export("hsStateHsciIntroSent")]
		string HsStateHsciIntroSent { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHsciDone;
		[Static]
		[Export("hsStateHsciDone")]
		string HsStateHsciDone { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHscrConnecting;
		[Static]
		[Export("hsStateHscrConnecting")]
		string HsStateHscrConnecting { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHscrEstablishedIdle;
		[Static]
		[Export("hsStateHscrEstablishedIdle")]
		string HsStateHscrEstablishedIdle { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHscrEstablishedWaiting;
		[Static]
		[Export("hsStateHscrEstablishedWaiting")]
		string HsStateHscrEstablishedWaiting { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHscrJoined;
		[Static]
		[Export("hsStateHscrJoined")]
		string HsStateHscrJoined { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHssiConnecting;
		[Static]
		[Export("hsStateHssiConnecting")]
		string HsStateHssiConnecting { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHssiEstablished;
		[Static]
		[Export("hsStateHssiEstablished")]
		string HsStateHssiEstablished { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHssrConnecting;
		[Static]
		[Export("hsStateHssrConnecting")]
		string HsStateHssrConnecting { get; }

		// @property (readonly, class) NSString * _Nonnull hsStateHssrJoined;
		[Static]
		[Export("hsStateHssrJoined")]
		string HsStateHssrJoined { get; }

		// @property (readonly, class) NSString * _Nonnull reasonNone;
		[Static]
		[Export("reasonNone")]
		string ReasonNone { get; }

		// @property (readonly, class) NSString * _Nonnull reasonTorProtocol;
		[Static]
		[Export("reasonTorProtocol")]
		string ReasonTorProtocol { get; }

		// @property (readonly, class) NSString * _Nonnull reasonInternal;
		[Static]
		[Export("reasonInternal")]
		string ReasonInternal { get; }

		// @property (readonly, class) NSString * _Nonnull reasonRequested;
		[Static]
		[Export("reasonRequested")]
		string ReasonRequested { get; }

		// @property (readonly, class) NSString * _Nonnull reasonHibernating;
		[Static]
		[Export("reasonHibernating")]
		string ReasonHibernating { get; }

		// @property (readonly, class) NSString * _Nonnull reasonResourceLimit;
		[Static]
		[Export("reasonResourceLimit")]
		string ReasonResourceLimit { get; }

		// @property (readonly, class) NSString * _Nonnull reasonConnectFailed;
		[Static]
		[Export("reasonConnectFailed")]
		string ReasonConnectFailed { get; }

		// @property (readonly, class) NSString * _Nonnull reasonOrIdentity;
		[Static]
		[Export("reasonOrIdentity")]
		string ReasonOrIdentity { get; }

		// @property (readonly, class) NSString * _Nonnull reasonOrConnClosed;
		[Static]
		[Export("reasonOrConnClosed")]
		string ReasonOrConnClosed { get; }

		// @property (readonly, class) NSString * _Nonnull reasonTimeout;
		[Static]
		[Export("reasonTimeout")]
		string ReasonTimeout { get; }

		// @property (readonly, class) NSString * _Nonnull reasonFinished;
		[Static]
		[Export("reasonFinished")]
		string ReasonFinished { get; }

		// @property (readonly, class) NSString * _Nonnull reasonDestroyed;
		[Static]
		[Export("reasonDestroyed")]
		string ReasonDestroyed { get; }

		// @property (readonly, class) NSString * _Nonnull reasonNoPath;
		[Static]
		[Export("reasonNoPath")]
		string ReasonNoPath { get; }

		// @property (readonly, class) NSString * _Nonnull reasonNoSuchService;
		[Static]
		[Export("reasonNoSuchService")]
		string ReasonNoSuchService { get; }

		// @property (readonly, class) NSString * _Nonnull reasonMeasurementExpired;
		[Static]
		[Export("reasonMeasurementExpired")]
		string ReasonMeasurementExpired { get; }

		// @property (readonly) NSString * _Nullable raw;
		[NullAllowed, Export("raw")]
		string Raw { get; }

		// @property (readonly) NSString * _Nullable circuitId;
		[NullAllowed, Export("circuitId")]
		string CircuitId { get; }

		// @property (readonly) NSString * _Nullable status;
		[NullAllowed, Export("status")]
		string Status { get; }

		// @property (readonly) NSArray<TORNode *> * _Nullable nodes;
		[NullAllowed, Export("nodes")]
		TORNode[] Nodes { get; }

		// @property (readonly) NSArray<NSString *> * _Nullable buildFlags;
		[NullAllowed, Export("buildFlags")]
		string[] BuildFlags { get; }

		// @property (readonly) NSString * _Nullable purpose;
		[NullAllowed, Export("purpose")]
		string Purpose { get; }

		// @property (readonly) NSString * _Nullable hsState;
		[NullAllowed, Export("hsState")]
		string HsState { get; }

		// @property (readonly) NSString * _Nullable rendQuery;
		[NullAllowed, Export("rendQuery")]
		string RendQuery { get; }

		// @property (readonly) NSDate * _Nullable timeCreated;
		[NullAllowed, Export("timeCreated")]
		NSDate TimeCreated { get; }

		// @property (readonly) NSString * _Nullable reason;
		[NullAllowed, Export("reason")]
		string Reason { get; }

		// @property (readonly) NSString * _Nullable remoteReason;
		[NullAllowed, Export("remoteReason")]
		string RemoteReason { get; }

		// @property (readonly) NSString * _Nullable socksUsername;
		[NullAllowed, Export("socksUsername")]
		string SocksUsername { get; }

		// @property (readonly) NSString * _Nullable socksPassword;
		[NullAllowed, Export("socksPassword")]
		string SocksPassword { get; }

		// +(NSArray<TORCircuit *> * _Nonnull)circuitsFromString:(NSString * _Nonnull)circuitsString;
		[Static]
		[Export("circuitsFromString:")]
		TORCircuit[] CircuitsFromString(string circuitsString);

		// -(instancetype _Nonnull)initFromString:(NSString * _Nonnull)circuitString;
		[Export("initFromString:")]
		IntPtr Constructor(string circuitString);
	}

	// This is our trampoline boy
	// typedef BOOL (^TORObserverBlock)(NSArray<NSNumber *> * _Nonnull, NSArray<NSData *> * _Nonnull, BOOL * _Nonnull);
	unsafe delegate bool TORObserverBlock(NSNumber[] arg0, NSData[] arg1, bool* arg2);

	[Static]
	//[Verify(ConstantsInterfaceAssociation)]
	partial interface Constants
	{
		// extern const NSErrorDomain _Nonnull TORControllerErrorDomain __attribute__((visibility("default")));
		[Field("TORControllerErrorDomain", "__Internal")]
		NSString TORControllerErrorDomain { get; }
	}

	// @interface TORController : NSObject
	[BaseType(typeof(NSObject))]
	[DisableDefaultCtor]
	interface TORController
	{
		// @property (readonly, copy, nonatomic) NSOrderedSet<NSString *> * _Nonnull events;
		[Export("events", ArgumentSemantic.Copy)]
		NSOrderedSet<NSString> Events { get; }

		// @property (readonly, getter = isConnected, nonatomic) BOOL connected;
		[Export("connected")]
		bool Connected { [Bind("isConnected")] get; }

		// -(instancetype _Nonnull)initWithSocketURL:(NSURL * _Nonnull)url __attribute__((objc_designated_initializer));
		[Export("initWithSocketURL:")]
		[DesignatedInitializer]
		IntPtr Constructor(NSUrl url);

		// -(instancetype _Nonnull)initWithSocketHost:(NSString * _Nonnull)host port:(in_port_t)port __attribute__((objc_designated_initializer));
		[Export("initWithSocketHost:port:")]
		[DesignatedInitializer]
		IntPtr Constructor(string host, ushort port);

		// -(BOOL)connect:(NSError * _Nullable * _Nullable)error;
		[Export("connect:")]
		bool Connect([NullAllowed] out NSError error);

		// -(void)disconnect;
		[Export("disconnect")]
		void Disconnect();

		// -(void)authenticateWithData:(NSData * _Nonnull)data completion:(void (^ _Nullable)(BOOL, NSError * _Nullable))completion;
		[Export("authenticateWithData:completion:")]
		[Async]
		void AuthenticateWithData(NSData data, [NullAllowed] Action<bool, NSError> completion);

		// -(void)resetConfForKey:(NSString * _Nonnull)key completion:(void (^ _Nullable)(BOOL, NSError * _Nullable))completion;
		[Export("resetConfForKey:completion:")]
		[Async]
		void ResetConfForKey(string key, [NullAllowed] Action<bool, NSError> completion);

		// -(void)setConfForKey:(NSString * _Nonnull)key withValue:(NSString * _Nonnull)value completion:(void (^ _Nullable)(BOOL, NSError * _Nullable))completion;
		[Export("setConfForKey:withValue:completion:")]
		[Async]
		void SetConfForKey(string key, string value, [NullAllowed] Action<bool, NSError> completion);

		// -(void)setConfs:(NSArray<NSDictionary *> * _Nonnull)configs completion:(void (^ _Nullable)(BOOL, NSError * _Nullable))completion;
		[Export("setConfs:completion:")]
		[Async]
		void SetConfs(NSDictionary[] configs, [NullAllowed] Action<bool, NSError> completion);

		// -(void)listenForEvents:(NSArray<NSString *> * _Nonnull)events completion:(void (^ _Nullable)(BOOL, NSError * _Nullable))completion;
		[Export("listenForEvents:completion:")]
		[Async]
		void ListenForEvents(string[] events, [NullAllowed] Action<bool, NSError> completion);

		// -(void)getInfoForKeys:(NSArray<NSString *> * _Nonnull)keys completion:(void (^ _Nonnull)(NSArray<NSString *> * _Nonnull))completion;
		[Export("getInfoForKeys:completion:")]
		[Async]
		void GetInfoForKeys(string[] keys, Action<NSArray<NSString>> completion);

		// -(void)getSessionConfiguration:(void (^ _Nonnull)(NSURLSessionConfiguration * _Nullable))completion;
		[Export("getSessionConfiguration:")]
		[Async]
		void GetSessionConfiguration(Action<NSUrlSessionConfiguration> completion);

		// TODO Fix: /Users/dan/Desktop/root/chaincase/app/TorFramework/BTOUCH: Error BI1001: bgen: Do not know how to make a trampoline for System.Boolean* (BI1001) (TorFramework)
		//[Export("sendCommand:arguments:data:observer:")]
		//void SendCommand(string command, [NullAllowed] string[] arguments, [NullAllowed] NSData data, TORObserverBlock observer);

		// -(void)getCircuits:(void (^ _Nonnull)(NSArray<TORCircuit *> * _Nonnull))completion;
		[Export("getCircuits:")]
		[Async]
        void GetCircuits(Action<NSArray<TORCircuit>> completion);

        // -(void)resetConnection:(void (^ _Nullable)(BOOL))completion;
        [Export("resetConnection:")]
        void ResetConnection([NullAllowed] Action<bool> completion);

        // -(void)closeCircuitsByIds:(NSArray<NSString *> * _Nonnull)circuitIds completion:(void (^ _Nullable)(BOOL))completion;
        [Export("closeCircuitsByIds:completion:")]
        void CloseCircuitsByIds(string[] circuitIds, [NullAllowed] Action<bool> completion);

        // -(void)closeCircuits:(NSArray<TORCircuit *> * _Nonnull)circuits completion:(void (^ _Nullable)(BOOL))completion;
        [Export("closeCircuits:completion:")]
        void CloseCircuits(TORCircuit[] circuits, [NullAllowed] Action<bool> completion);


        // -(id _Nonnull)addObserverForCircuitEstablished:(void (^ _Nonnull)(BOOL))block;
        [Export("addObserverForCircuitEstablished:")]
        NSObject AddObserverForCircuitEstablished(Action<bool> block);

        // -(id _Nonnull)addObserverForStatusEvents:(BOOL (^ _Nonnull)(NSString * _Nonnull, NSString * _Nonnull, NSString * _Nonnull, NSDictionary<NSString *,NSString *> * _Nullable))block;
        [Export("addObserverForStatusEvents:")]
        NSObject AddObserverForStatusEvents(Func<NSString, NSString, NSString, NSDictionary<NSString, NSString>, bool> block);

        // -(void)removeObserver:(id _Nullable)observer;
        [Export("removeObserver:")]
        void RemoveObserver([NullAllowed] NSObject observer);
    }
	
	// @interface TORConfiguration : NSObject
	[BaseType(typeof(NSObject))]
	interface TORConfiguration
	{
		// @property (copy, nonatomic) NSURL * _Nullable dataDirectory;
		[NullAllowed, Export("dataDirectory", ArgumentSemantic.Copy)]
		NSUrl DataDirectory { get; set; }

		// @property (copy, nonatomic) NSURL * _Nullable controlSocket;
		[NullAllowed, Export("controlSocket", ArgumentSemantic.Copy)]
		NSUrl ControlSocket { get; set; }

		// @property (copy, nonatomic) NSURL * _Nullable socksURL;
		[NullAllowed, Export("socksURL", ArgumentSemantic.Copy)]
		NSUrl SocksURL { get; set; }

		// @property (copy, nonatomic) NSNumber * _Nullable cookieAuthentication;
		[NullAllowed, Export("cookieAuthentication", ArgumentSemantic.Copy)]
		NSNumber CookieAuthentication { get; set; }

		// @property (copy, nonatomic) NSDictionary<NSString *,NSString *> * _Null_unspecified options;
		[Export("options", ArgumentSemantic.Copy)]
		NSDictionary<NSString, NSString> Options { get; set; }

		// @property (copy, nonatomic) NSArray<NSString *> * _Null_unspecified arguments;
		[Export("arguments", ArgumentSemantic.Copy)]
		string[] Arguments { get; set; }
	}

	// @interface TORThread : NSThread
	[BaseType(typeof(NSThread))]
	interface TORThread
	{
		// @property (readonly, class) TORThread * _Nullable activeThread;
		[Static]
		[NullAllowed, Export("activeThread")]
		TORThread ActiveThread { get; }

		// -(instancetype _Nonnull)initWithConfiguration:(TORConfiguration * _Nullable)configuration;
		[Export("initWithConfiguration:")]
		IntPtr Constructor([NullAllowed] TORConfiguration configuration);

		// -(instancetype _Nonnull)initWithArguments:(NSArray<NSString *> * _Nullable)arguments __attribute__((objc_designated_initializer));
		[Export("initWithArguments:")]
		[DesignatedInitializer]
		IntPtr Constructor([NullAllowed] string[] arguments);
	}
}

