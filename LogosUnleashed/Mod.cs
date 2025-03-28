using ff16.gameplay.logosunleashed.Configuration;
using ff16.gameplay.logosunleashed.Template;
using static ff16.gameplay.logosunleashed.Configuration.Config;

using Reloaded.Mod.Interfaces;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

using FF16Framework.Interfaces.Nex;
using FF16Tools.Files.Nex;
using FF16Tools.Files.Nex.Entities;

using SharpDX.XInput;
using GlobalKeyInterceptor;
using DualSenseAPI;

namespace ff16.gameplay.logosunleashed;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    public readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    public readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    public IModConfig _modConfig { get; set; }

    public WeakReference<INextExcelDBApiManaged>? _nexApi;

    public Controller controller = new Controller(UserIndex.One);

    public DualSense? dualSense;
    public bool isPolling = false;

    public static NexTableLayout summonmodeLayout = TableMappingReader.ReadTableLayout("summonmode", new Version(1, 0, 0));
    public static NexTableLayout playercommandbuilderLayout = TableMappingReader.ReadTableLayout("playercommandbuilder", new Version(1, 0, 0));
    public static NexTableLayout actionLayout = TableMappingReader.ReadTableLayout("action", new Version(1, 0, 0));
    public static NexTableLayout comboLayout = TableMappingReader.ReadTableLayout("combo", new Version(1, 0, 0));
    // public static NexTableLayout weaponbaseLayout = TableMappingReader.ReadTableLayout("weaponbase", new Version(1, 0, 0));
    public static NexTableLayout systemmoveLayout = TableMappingReader.ReadTableLayout("systemmove", new Version(1, 0, 0));

    public static INexTable? summonmodeTable;
    public static INexTable? playercommandbuilderTable;
    public static INexTable? actionTable;
    public static INexTable? comboTable;
    // public static INexTable? weaponbaseTable;
    public static INexTable? systemmoveTable;
    
    public static bool isComboFinisherActive = false;

    public static int[] groundLogosVanilla = { 139, 358, 4602, 1162 };
    public static int[] airLogosVanilla = { 297, 298, 299};
    public static int[] groundLogosSpin = { 143, 143, 183, 183 };
    public static int[] groundLogosStinger = { 163, 163, 163, 163 };

    public static int[] groundLogosBurstVanilla = { 199, 200, 201, 202 };
    public static int[] airLogosBurstVanilla = { 424, 424, 424 };
    public static int[] groundLogosBurstLauncher = { 175, 175, 175, 175 };
    public static int[] airLogosBurstLauncher = { 463, 463, 463 };
    public static int[] airLogosBurstGlide = { 871, 871, 871 };

    public static int[] groundLogosVanillaPostburst = [0, 154, 155, 296];
    public static int[] groundLogosAlternatePostburst = [0, 0, 0, 0];

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;
        
        _logger.WriteLine($"[{_modConfig.ModId}] Initializing..", _logger.ColorGreen);

        _nexApi = _modLoader.GetController<INextExcelDBApiManaged>();
        if (_nexApi is null || !_nexApi.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?", _logger.ColorRed);
            return;
        }

        nextExcelDBApi.OnNexLoaded += NextExcelDBApi_OnNexLoaded;
    }

    /// <summary>
    /// Fired when the game has loaded all nex tables.
    /// </summary>
    private void NextExcelDBApi_OnNexLoaded()
    {
        if (_nexApi is null || !_nexApi.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?", _logger.ColorRed);
            return;
        }

        summonmodeTable = nextExcelDBApi.GetTable(NexTableIds.summonmode)!;
        playercommandbuilderTable = nextExcelDBApi.GetTable(NexTableIds.playercommandbuilder)!;
        actionTable = nextExcelDBApi.GetTable(NexTableIds.action)!;
        comboTable = nextExcelDBApi.GetTable(NexTableIds.combo)!;
        // weaponbaseTable = nextExcelDBApi.GetTable(NexTableIds.weaponbase)!;
        systemmoveTable = nextExcelDBApi.GetTable(NexTableIds.systemmove);

        if (_configuration.isKeyboardEnabled)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Keyboard input has been enabled. Send your feedback on the nexus!", _logger.ColorYellow);
            Thread keyboardInputThread = new Thread(new ThreadStart(KeyboardInputThread));
            keyboardInputThread.IsBackground = true;
            keyboardInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] Keyboard support is disabled. You can enable it in mod configuration menu.", _logger.ColorYellow);

        if (_configuration.isControllerEnabled)
        {
            Thread controllerInputThread = new Thread(new ThreadStart(ControllerInputThread));
            controllerInputThread.IsBackground = true;
            controllerInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] XInput Gamepad support has been disabled. You can re-enable it in mod configuration menu.", _logger.ColorYellow);

        if (_configuration.isDualSenseEnabled)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Experimental DualSense input has been enabled. Not recommended! Use a XInput emulator instead.", _logger.ColorYellow);
            Thread dualSenseInputThread = new Thread(new ThreadStart(DualSenseInputThread));
            dualSenseInputThread.IsBackground = true;
            dualSenseInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] DualSense support is disabled. You can enable it in mod configuration menu.", _logger.ColorYellow);
        
        if (!_configuration.isKeyboardEnabled && !_configuration.isControllerEnabled && !_configuration.isDualSenseEnabled)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Support for all input devices is disabled. Mod will not work properly.", _logger.ColorRed);
        }

        ReplaceComboStrings();
    }

    private void KeyboardInputThread()
    {
        Shortcut[] shortcuts =
        [
            new Shortcut(Key.W, state: KeyState.Down, name: "W Down"),
            new Shortcut(Key.S, state: KeyState.Down, name: "S Down"),
            new Shortcut(_configuration.specialAttackKeybindKeyboard, state: KeyState.Down, name: "CapsLock Down"),
            new Shortcut(Key.W, state: KeyState.Up, name: "W Up"),
            new Shortcut(Key.S, state: KeyState.Up, name: "S Up"),
            new Shortcut(_configuration.specialAttackKeybindKeyboard, state: KeyState.Up, name: "CapsLock Up"),

            new Shortcut(_configuration.alternateComboKeybindKeyboard, state: KeyState.Down, name: "X Down"),
            new Shortcut(_configuration.alternateComboKeybindKeyboard, state: KeyState.Up, name: "X Up"),

            new Shortcut(_configuration.flyUpKeybindKeyboard, state: KeyState.Down, name: "FlyUp Down"),
            new Shortcut(_configuration.flyUpKeybindKeyboard, state: KeyState.Up, name: "FlyUp Up"),
            new Shortcut(_configuration.flyDownKeybindKeyboard, state: KeyState.Down, name: "FlyDown Down"),
            new Shortcut(_configuration.flyDownKeybindKeyboard, state: KeyState.Up, name: "FlyDown Up"),
        ];

        var interceptor = new KeyInterceptor(shortcuts);

        bool wDown = false;
        bool sDown = false;
        bool clDown = false;

        interceptor.ShortcutPressed += (_, e) =>
        {
            switch (e.Shortcut.Name)
            {
                case "W Down":
                    if (!clDown && !sDown)
                    {
                        DoUpAttack();
                        wDown = true;
                    }
                    break;
                case "S Down":
                    if (!clDown && !wDown)
                    {
                        sDown = true;
                        DoDownAttack();
                    }
                    break;
                case "CapsLock Down":
                    clDown = true;
                    DoSpecialAttack();
                    break;
                case "W Up":
                    if (!clDown && !sDown)
                    {
                        wDown = false;
                        UndoUpAttack();
                    }
                    break;
                case "S Up":
                    if (!clDown && !wDown)
                    {
                        sDown = false;
                        UndoDownAttack();
                    }
                    break;
                case "CapsLock Up":
                    clDown = false;
                    UndoSpecialAttack();
                    break;
                
                case "X Down":
                    DoComboFinisher();
                    break;
                case "X Up":
                    UndoComboFinisher();
                    break;

                // case "FlyUp Down":
                //     DoFlyUp();
                //     break;
                // case "FlyUp Up":
                //     UndoFlyUp();
                //     break;
                case "FlyDown Down":
                    DoFlyDown();
                    break;
                case "FlyDown Up":
                    UndoFlyDown();
                    break;
            }
        };

        interceptor.RunMessageLoop();
    }

    private void ControllerInputThread()
    {
        if (!controller.IsConnected)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] XInput Gamepad not found. Is it compatible with XInput?", _logger.ColorYellow);
        }

        GamepadButtonFlags previousButtons = 0;
        //byte previousLeftTrigger = 0;
        byte previousTrigger = 0;
        int previousLeftStickY = 0;

        int deadZoneVertical = _configuration.deadZoneVertical;
        int deadZoneR2 = _configuration.deadZoneR2;
        GamepadButtonFlags specialAttackKeybind = (GamepadButtonFlags)_configuration.specialAttackKeybind;
        GamepadButtonFlags alternateComboKeybind = (GamepadButtonFlags)_configuration.alternateComboKeybind;
        // GamepadButtonFlags switchStyleKeybind = GamepadButtonFlags.RightThumb;

        GamepadButtonFlags flyUpKeybind = (GamepadButtonFlags)_configuration.flyUpKeybindController;
        GamepadButtonFlags flyDownKeybind = (GamepadButtonFlags)_configuration.flyDownKeybindController;

        if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger || _configuration.specialAttackKeybind == ButtonTriggerFlags.RightTrigger)
        {
            while(true)
            {
                if (controller.IsConnected)
                {   
                    var state = controller.GetState();
    
                    GamepadButtonFlags currentButtons = state.Gamepad.Buttons;
                    byte currentTrigger;
                    int currentLeftStickY = state.Gamepad.LeftThumbY;

                    if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger)
                        currentTrigger = state.Gamepad.LeftTrigger;
                    else
                        currentTrigger = state.Gamepad.RightTrigger;
    
                    if (currentTrigger > deadZoneR2)
                    {
                        // if ((currentButtons & switchStyleKeybind) != 0 && (previousButtons & switchStyleKeybind) == 0)
                        // {
                        //     _logger.WriteLine("RT + Right Thumb just pressed!");
                        //     SwitchStyle();
                        // }
    
                        if (previousTrigger <= deadZoneR2)
                        {
                            //_logger.WriteLine("RT just pressed!");
                            DoSpecialAttack();

                            // if ((currentButtons & flyUpKeybind) == 0 && (previousButtons & flyUpKeybind) != 0)
                            // {
                            //     DoFlyUp();
                            // }
                            // if ((currentButtons & flyDownKeybind) == 0 && (previousButtons & flyDownKeybind) != 0)
                            // {
                            //     DoFlyDown();
                            // }
                        }
                    }
                    else
                    {
                        if (previousTrigger > deadZoneR2)
                        {
                            //_logger.WriteLine("RT just released!");
                            UndoSpecialAttack();

                            // UndoFlyUp();
                            // UndoFlyDown();
                        }
                    }
    
                    if (currentLeftStickY > deadZoneVertical && currentTrigger <= deadZoneR2)
                    {
                        // _logger.WriteLine("Up pressed");
                        DoUpAttack();
                    }
                    else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY > deadZoneVertical)
                    {
                        // _logger.WriteLine("Up just released");
                        UndoUpAttack();
                    }
                    
                    if (currentLeftStickY < deadZoneVertical * (-1) && currentTrigger <= deadZoneR2)
                    {
                        // _logger.WriteLine("Down pressed");
                        DoDownAttack();
                    }
                    else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1))
                    {
                        // _logger.WriteLine("Down just released");
                        UndoDownAttack();
                    }
    
                    if ((currentButtons & alternateComboKeybind) != 0 && (previousButtons & alternateComboKeybind) == 0)
                    {
                        DoComboFinisher();
                    }
                    else if ((currentButtons & alternateComboKeybind) == 0 && (previousButtons & alternateComboKeybind) != 0)
                    {
                        UndoComboFinisher();
                    }

                    if ((currentButtons & flyDownKeybind) != 0 && (previousButtons & flyDownKeybind) == 0)
                    {
                        DoFlyDown();
                    }
                    if ((currentButtons & flyDownKeybind) == 0 && (previousButtons & flyDownKeybind) != 0)
                    {
                        UndoFlyDown();
                    }
    
                    previousButtons = currentButtons;
                    // previousLeftTrigger = currentLeftTrigger;
                    previousTrigger = currentTrigger;
                    previousLeftStickY = currentLeftStickY;
                }
                else
                {
                    Thread.Sleep(1000); //Prevent stuttering
                }
            }
        }
        else
        {
            while(true)
            {
                if (controller.IsConnected)
                {
                    var state = controller.GetState();
    
                    GamepadButtonFlags currentButtons = state.Gamepad.Buttons;
                    int currentLeftStickY = state.Gamepad.LeftThumbY;
    
                    if ((currentButtons & specialAttackKeybind) != 0)
                    {
                        DoSpecialAttack();

                        // if ((currentButtons & flyUpKeybind) == 0 && (previousButtons & flyUpKeybind) != 0)
                        // {
                        //     DoFlyUp();
                        // }
                        // if ((currentButtons & flyDownKeybind) == 0 && (previousButtons & flyDownKeybind) != 0)
                        // {
                        //     DoFlyDown();
                        // }
                    }
                    else if ((previousButtons & specialAttackKeybind) != 0)
                    {
                        UndoSpecialAttack();

                        // UndoFlyUp();
                        // UndoFlyDown();
                    }
    
                    if (currentLeftStickY > deadZoneVertical && (currentButtons & specialAttackKeybind) == 0)
                    {
                        // _logger.WriteLine("Up pressed");
                        DoUpAttack();
                    }
                    else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY > deadZoneVertical)
                    {
                        // _logger.WriteLine("Up just released");
                        UndoUpAttack();
                    }
                    
                    if (currentLeftStickY < deadZoneVertical * (-1) && (currentButtons & specialAttackKeybind) == 0)
                    {
                        // _logger.WriteLine("Down pressed");
                        DoDownAttack();
                    }
                    else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1))
                    {
                        // _logger.WriteLine("Down just released");
                        UndoDownAttack();
                    }

                    if ((currentButtons & alternateComboKeybind) != 0 && (previousButtons & alternateComboKeybind) == 0)
                    {
                        DoComboFinisher();
                    }
                    else if ((currentButtons & alternateComboKeybind) == 0 && (previousButtons & alternateComboKeybind) != 0)
                    {
                        UndoComboFinisher();
                    }

                    if ((currentButtons & flyUpKeybind) != 0 && (previousButtons & flyUpKeybind) == 0)
                    {
                        DoFlyDown();
                    }
                    if ((currentButtons & flyDownKeybind) == 0 && (previousButtons & flyDownKeybind) != 0)
                    {
                        UndoFlyDown();
                    }
    
                    previousButtons = currentButtons;
                    previousLeftStickY = currentLeftStickY;
                }
                else
                {
                    Thread.Sleep(1000); //Prevent stuttering
                }
            }
        }
    }

    private void DualSenseInputThread()
    {
        if (DualSense.EnumerateControllers().Count() == 0)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] DualSense not found.", _logger.ColorYellow);
        }
        else
        {
            dualSense = DualSense.EnumerateControllers().First();
        }

        bool previousSpecialButton = false;
        bool previousFinisherButton = false;
        float previousTrigger = 0.0f;
        float previousLeftStickY = 0.0f;

        bool previousFlyUp = false;
        bool previousFlyDown = false;

        float deadZoneVertical = _configuration.deadZoneVerticalDualSense;
        float deadZoneR2 = _configuration.deadZoneR2DualSense;
        ButtonTriggerFlags specialAttackKeybind = _configuration.specialAttackKeybind;
        ButtonFlags alternateComboKeybind = _configuration.alternateComboKeybind;

        if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger || _configuration.specialAttackKeybind == ButtonTriggerFlags.RightTrigger)
        {
            while(true)
            {
                if (DualSense.EnumerateControllers().Count() > 0)
                {
                    _logger.WriteLine($"[{_modConfig.ModId}] DualSense connected.");
                    
                    dualSense = DualSense.EnumerateControllers().First();

                    dualSense!.Acquire();

                    dualSense.OnStatePolled += (sender) =>
                    {
                        var state = sender.InputState;

                        float currentLeftStickY = state.LeftAnalogStick.Y;
                        float currentTrigger;
                        bool currentFinisherButton = false;

                        bool currentFlyUp = false;
                        bool currentFlyDown = false;

                        if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger)
                            currentTrigger = state.L2;
                        else
                            currentTrigger = state.R2;
                        
                        switch (_configuration.alternateComboKeybind)
                        {
                            case ButtonFlags.DPadUp:
                                currentFinisherButton = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFinisherButton = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFinisherButton = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFinisherButton = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFinisherButton = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFinisherButton = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFinisherButton = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFinisherButton = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFinisherButton = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFinisherButton = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFinisherButton = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFinisherButton = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFinisherButton = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFinisherButton = state.CircleButton;
                                break;
                        }

                        switch (_configuration.flyUpKeybindController)
                        {
                            case ButtonFlags.DPadUp:
                                currentFlyUp = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFlyUp = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFlyUp = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFlyUp = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFlyUp = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFlyUp = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFlyUp = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFlyUp = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFlyUp = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFlyUp = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFlyUp = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFlyUp = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFlyUp = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFlyUp = state.CircleButton;
                                break;
                        }

                        switch (_configuration.flyDownKeybindController)
                        {
                            case ButtonFlags.DPadUp:
                                currentFlyDown = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFlyDown = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFlyDown = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFlyDown = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFlyDown = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFlyDown = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFlyDown = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFlyDown = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFlyDown = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFlyDown = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFlyDown = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFlyDown = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFlyDown = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFlyDown = state.CircleButton;
                                break;
                        }
                        
                        if (currentTrigger > deadZoneR2)
                        {
                            if (previousTrigger <= deadZoneR2)
                            {
                                // _logger.WriteLine("RT just pressed!");
                                DoSpecialAttack();

                                // if (currentFlyUp && !previousFlyUp)
                                // {
                                //     DoFlyUp();
                                // }
                                // if (!currentFlyUp && previousFlyUp)
                                // {
                                //     UndoFlyUp();
                                // }
                            }
                        }
                        else
                        {
                            if (previousTrigger > deadZoneR2)
                            {
                                // _logger.WriteLine("RT just released!");
                                UndoSpecialAttack();
                                    
                                // UndoFlyUp();
                                // UndoFlyDown();
                            }
                        }
        
                        if (currentLeftStickY > deadZoneVertical && currentTrigger <= deadZoneR2)
                        {
                            // _logger.WriteLine("Up pressed");
                            DoUpAttack();
                        }
                        else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY > deadZoneVertical)
                        {
                            // _logger.WriteLine("Up just released");
                            UndoUpAttack();
                        }
                        
                        if (currentLeftStickY < deadZoneVertical * (-1) && currentTrigger <= deadZoneR2)
                        {
                            // _logger.WriteLine("Down pressed");
                            DoDownAttack();
                        }
                        else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1))
                        {
                            // _logger.WriteLine("Down just released");
                            UndoDownAttack();
                        }
        
                        if (currentFinisherButton && !previousFinisherButton)
                        {
                            DoComboFinisher();
                        }
                        else if (!currentFinisherButton && previousFinisherButton)
                        {
                            UndoComboFinisher();
                        }

                        if (currentFlyDown && !previousFlyDown)
                        {
                            DoFlyDown();
                        }
                        if (!currentFlyDown && previousFlyDown)
                        {
                            UndoFlyDown();
                        }
        
                        previousFinisherButton = currentFinisherButton;
                        previousTrigger = currentTrigger;
                        previousLeftStickY = currentLeftStickY;
                        previousFlyDown = currentFlyDown;
                    };
                    if (!isPolling)
                        dualSense.BeginPolling(20);
                    isPolling = true;
                    while (DualSense.EnumerateControllers().Count() > 0)
                        Console.ReadLine();
                    dualSense.EndPolling();
                    dualSense.Release();
                }
                else
                {
                    Thread.Sleep(1000); //Prevent stuttering
                }
            }
        }
        else
        {
            while(true)
            {
                if (DualSense.EnumerateControllers().Count() > 0)
                {
                    _logger.WriteLine($"[{_modConfig.ModId}] DualSense connected.");

                    dualSense = DualSense.EnumerateControllers().First();

                    dualSense.Acquire();

                    dualSense.OnStatePolled += (sender) =>
                    {
                        var state = sender.InputState;

                        float currentLeftStickY = state.LeftAnalogStick.Y;
                        bool currentFinisherButton = false;
                        bool currentSpecialButton = false;

                        bool currentFlyUp = false;
                        bool currentFlyDown = false;
                        
                        switch (_configuration.specialAttackKeybind)
                        {
                            case ButtonTriggerFlags.DPadUp:
                                currentSpecialButton = state.DPadUpButton;
                                break;
                            case ButtonTriggerFlags.DPadDown:
                                currentSpecialButton = state.DPadDownButton;
                                break;
                            case ButtonTriggerFlags.DPadLeft:
                                currentSpecialButton = state.DPadLeftButton;
                                break;
                            case ButtonTriggerFlags.DPadRight:
                                currentSpecialButton = state.DPadRightButton;
                                break;
                            case ButtonTriggerFlags.LeftBumper:
                                currentSpecialButton = state.L1Button;
                                break;
                            case ButtonTriggerFlags.RightBumper:
                                currentSpecialButton = state.R1Button;
                                break;
                            case ButtonTriggerFlags.LeftThumb:
                                currentSpecialButton = state.L3Button;
                                break;
                            case ButtonTriggerFlags.RightThumb:
                                currentSpecialButton = state.R3Button;
                                break;
                            case ButtonTriggerFlags.Start:
                                currentSpecialButton = state.MenuButton;
                                break;
                            case ButtonTriggerFlags.Select:
                                currentSpecialButton = state.TouchpadButton;
                                break;
                            case ButtonTriggerFlags.X_Square:
                                currentSpecialButton = state.SquareButton;
                                break;
                            case ButtonTriggerFlags.Y_Triangle:
                                currentSpecialButton = state.TriangleButton;
                                break;
                            case ButtonTriggerFlags.A_Cross:
                                currentSpecialButton = state.CrossButton;
                                break;
                            case ButtonTriggerFlags.B_Circle:
                                currentSpecialButton = state.CircleButton;
                                break;
                        }

                        switch (_configuration.alternateComboKeybind)
                        {
                            case ButtonFlags.DPadUp:
                                currentFinisherButton = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFinisherButton = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFinisherButton = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFinisherButton = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFinisherButton = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFinisherButton = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFinisherButton = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFinisherButton = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFinisherButton = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFinisherButton = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFinisherButton = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFinisherButton = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFinisherButton = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFinisherButton = state.CircleButton;
                                break;
                        }

                        switch (_configuration.flyUpKeybindController)
                        {
                            case ButtonFlags.DPadUp:
                                currentFlyUp = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFlyUp = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFlyUp = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFlyUp = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFlyUp = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFlyUp = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFlyUp = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFlyUp = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFlyUp = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFlyUp = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFlyUp = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFlyUp = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFlyUp = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFlyUp = state.CircleButton;
                                break;
                        }

                        switch (_configuration.flyDownKeybindController)
                        {
                            case ButtonFlags.DPadUp:
                                currentFlyDown = state.DPadUpButton;
                                break;
                            case ButtonFlags.DPadDown:
                                currentFlyDown = state.DPadDownButton;
                                break;
                            case ButtonFlags.DPadLeft:
                                currentFlyDown = state.DPadLeftButton;
                                break;
                            case ButtonFlags.DPadRight:
                                currentFlyDown = state.DPadRightButton;
                                break;
                            case ButtonFlags.LeftBumper:
                                currentFlyDown = state.L1Button;
                                break;
                            case ButtonFlags.RightBumper:
                                currentFlyDown = state.R1Button;
                                break;
                            case ButtonFlags.LeftThumb:
                                currentFlyDown = state.L3Button;
                                break;
                            case ButtonFlags.RightThumb:
                                currentFlyDown = state.R3Button;
                                break;
                            case ButtonFlags.Start:
                                currentFlyDown = state.MenuButton;
                                break;
                            case ButtonFlags.Select:
                                currentFlyDown = state.TouchpadButton;
                                break;
                            case ButtonFlags.X_Square:
                                currentFlyDown = state.SquareButton;
                                break;
                            case ButtonFlags.Y_Triangle:
                                currentFlyDown = state.TriangleButton;
                                break;
                            case ButtonFlags.A_Cross:
                                currentFlyDown = state.CrossButton;
                                break;
                            case ButtonFlags.B_Circle:
                                currentFlyDown = state.CircleButton;
                                break;
                        }
                        
                        if (currentSpecialButton)
                        {
                            // _logger.WriteLine("RT just pressed!");
                            DoSpecialAttack();

                            // if (currentFlyUp && !previousFlyUp)
                            // {
                            //     DoFlyUp();
                            // }
                            // if (!currentFlyUp && previousFlyUp)
                            // {
                            //     UndoFlyUp();
                            // }
                        }
                        else if (previousSpecialButton)
                        {
                            // _logger.WriteLine("RT just released!");
                            UndoSpecialAttack();

                            // UndoFlyUp();
                            // UndoFlyDown();
                        }

                        if (currentLeftStickY > deadZoneVertical && !currentSpecialButton)
                        {
                            // _logger.WriteLine("Up pressed");
                            DoUpAttack();
                        }
                        else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY > deadZoneVertical)
                        {
                            // _logger.WriteLine("Up just released");
                            UndoUpAttack();
                        }
                        
                        if (currentLeftStickY < deadZoneVertical * (-1) && !currentSpecialButton)
                        {
                            // _logger.WriteLine("Down pressed");
                            DoDownAttack();
                        }
                        else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1))
                        {
                            // _logger.WriteLine("Down just released");
                            UndoDownAttack();
                        }
        
                        if (currentFinisherButton && !previousFinisherButton)
                        {
                            DoComboFinisher();
                        }
                        else if (!currentFinisherButton && previousFinisherButton)
                        {
                            UndoComboFinisher();
                        }

                        if (currentFlyDown && !previousFlyDown)
                        {
                            DoFlyDown();
                        }
                        if (!currentFlyDown && previousFlyDown)
                        {
                            UndoFlyDown();
                        }

                        previousSpecialButton = currentSpecialButton;
                        previousFinisherButton = currentFinisherButton;
                        previousLeftStickY = currentLeftStickY;
                        previousFlyDown = currentFlyDown;
                    };
                    dualSense.BeginPolling(20);
                    while (DualSense.EnumerateControllers().Count() > 0)
                        Console.ReadLine();
                    dualSense.EndPolling();
                    dualSense.Release();
                }
                else
                {
                    Thread.Sleep(1000); //Prevent stuttering
                }
            }
        }
    }

    private void DoUpAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Do Deadly Gambit
        INexRow? row = summonmodeTable!.GetRow(2);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3111)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3111);

        //Do Flash of Steel
        row = summonmodeTable.GetRow(7);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3109)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3109);

        //Do Serpent's Launcher
        row = summonmodeTable.GetRow(9);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3112)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3112);
        
        //Do Logos Shift
        row = summonmodeTable!.GetRow(10);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3126)
            row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3126);

        //ACTION
        column = "CharaTimelineId";

        //Do Ground Shift Shot -> Ground Ignition Finisher
        row = actionTable!.GetRow(360);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) != 2148)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2148);
        
        //Do Air Shift Shot -> Air Ignition Finisher
        row = actionTable!.GetRow(361);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) != 2149)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2149);

        //PLAYERCOMMANDBUILDER
        column = "Unk15";

        //Do Downthrust -> Lunge
        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            row = playercommandbuilderTable!.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 17)
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3119);
            else if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3122)
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3123);
        }

        //Do Logos Phoenix Shift
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) != 201)
            row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 201);

        //COMBO
        
        //Do Logos Stinger
        column = "Unk2";

        if (!isComboFinisherActive)
        {
            for (uint i = 0; i < 4; i++)
            {
                row = comboTable!.GetRow(30, i)!;

                row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosStinger[i]);

                row.SetInt32((uint)comboLayout.Columns["Unk21"].Offset, groundLogosAlternatePostburst[i]);
            }
        }

        //Do Logos Dark Glide
        column = "Unk9";

        for (uint i = 0; i < 3; i++)
        {
            row = comboTable!.GetRow(30, i)!;

            row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogosBurstGlide[i]);
        }
    }

    private void UndoUpAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Undo Deadly Gambit
        INexRow? row = summonmodeTable!.GetRow(2);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3111)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 222);

        //Undo Flash of Steel
        row = summonmodeTable.GetRow(7);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3109)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 281);

        //Undo Serpent's Launcher
        row = summonmodeTable.GetRow(9);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3112)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 341);
        
        //Undo Logos Shift
        row = summonmodeTable!.GetRow(10);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3126)
            row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3108);

        //ACTION
        column = "CharaTimelineId";

        //Undo Ground Shift Shot -> Ground Ignition Finisher
        row = actionTable!.GetRow(360);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) == 2148)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2174);
        
        //Undo Air Shift Shot -> Air Ignition Finisher
        row = actionTable!.GetRow(361);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) == 2149)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2175);

        //PLAYERCOMMANDBUILDER
        column = "Unk15";

        //Undo Downthrust -> Lunge
        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            row = playercommandbuilderTable!.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3119)
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 17);
            else if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3123)
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3122);
        }

        //Undo Logos Phoenix Shift
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 201)
            row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3121);

        //COMBO

        //Undo Logos Stinger
        column = "Unk2";

        if (!isComboFinisherActive)
        {
            for (uint i = 0; i < 4; i++)
            {
                row = comboTable!.GetRow(30, i)!;

                row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosVanilla[i]);

                row.SetInt32((uint)comboLayout.Columns["Unk21"].Offset, groundLogosVanillaPostburst[i]);
            }
        }

        //Undo Logos Dark Glide
        column = "Unk9";

        for (uint i = 0; i < 3; i++)
        {
            row = comboTable!.GetRow(30, i)!;

            row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogosBurstVanilla[i]);
        }
    }

    private void DoDownAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Do Deadly Wheel
        INexRow? row = summonmodeTable!.GetRow(2);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3110)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3110);

        //Do Mirage
        row = summonmodeTable!.GetRow(7);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3114)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3114);

        //Do Blind Drive
        row = summonmodeTable!.GetRow(4);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3115)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3115);

        //Do Serpent's Wisdom
        row = summonmodeTable!.GetRow(9);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3116)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3116);
        
        //Do Logos Embrace
        row = summonmodeTable!.GetRow(10);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) != 3127)
            row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3127);

        //ACTION
        column = "CharaTimelineId";

        //Do Ground Shift Shot -> Air Burning Blade
        row = actionTable!.GetRow(360);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) != 1467)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 1467);

        //Do Air Shift Shot -> LB Downthrust
        row = actionTable!.GetRow(361);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) != 1561)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 1561);
        
        //PLAYERCOMMANDBUILDER

        //Do Logos Deadly Embrace
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) != 222)
            row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 222);

        //COMBO

        //Do Logos Spin Combo
        if (!isComboFinisherActive)
        {
            column = "Unk2";

            for (uint i = 0; i < 4; i++)
            {
                row = comboTable!.GetRow(30, i)!;

                row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosSpin[i]);
            }
        }

        //Do Ground Logos Launcher Burst
        column = "Unk8";

        for (uint i = 0; i < 4; i++)
        {
            row = comboTable!.GetRow(30, i)!;
            
            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosBurstLauncher[i]);

            row.SetInt32((uint)comboLayout.Columns["Unk21"].Offset, groundLogosAlternatePostburst[i]);
        }

        //Do Air Logos Launcher Burst
        column = "Unk9";

        for (uint i = 0; i < 3; i++)
        {
            row = comboTable!.GetRow(30, i)!;
            
            row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogosBurstLauncher[i]);
        }
    }

    private void UndoDownAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Undo Deadly Wheel
        INexRow? row = summonmodeTable!.GetRow(2);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3110)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 222);

        //Undo Mirage
        row = summonmodeTable!.GetRow(7);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3114)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 281);

        //Undo Blind Drive
        row = summonmodeTable!.GetRow(4);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3115)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 321);

        //Undo Serpent's Wisdom
        row = summonmodeTable!.GetRow(9);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3116)
            row.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 341);
        
        //Undo Logos Embrace
        row = summonmodeTable!.GetRow(10);

        if (row!.GetInt32((uint)summonmodeLayout.Columns[column].Offset) == 3127)
            row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3108);

        //ACTION
        column = "CharaTimelineId";

        //Undo Ground Shift Shot -> Air Burning Blade
        row = actionTable!.GetRow(360);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) == 1467)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2174);

        //Undo Air Shift Shot -> LB Downthrust
        row = actionTable!.GetRow(361);

        if (row!.GetInt32((uint)actionLayout.Columns[column].Offset) == 1561)
            row.SetInt32((uint)actionLayout.Columns[column].Offset, 2175);

        //Undo Logos Deadly Embrace
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 222)
            row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3121);

        //COMBO

        //Undo Logos Spin Combo
        if (!isComboFinisherActive)
        {
            column = "Unk2";

            for (uint i = 0; i < 4; i++)
            {
                row = comboTable!.GetRow(30, i)!;

                row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosVanilla[i]);

                row.SetInt32((uint)comboLayout.Columns["Unk21"].Offset, groundLogosVanillaPostburst[i]);
            }
        }

        //Undo Ground Logos Launcher Burst
        column = "Unk8";

        for (uint i = 0; i < 4; i++)
        {
            row = comboTable!.GetRow(30, i)!;
            
            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosBurstVanilla[i]);
        }

        //Undo Air Logos Launcher Burst
        column = "Unk9";

        for (uint i = 0; i < 3; i++)
        {
            row = comboTable!.GetRow(30, i)!;
            
            row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogosBurstVanilla[i]);
        }
    }
    
    private void DoSpecialAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Phoenix Shift
        INexRow? row = summonmodeTable!.GetRow(1);   //Post LB Phoenix

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3106);

        row = summonmodeTable.GetRow(6);    //Pre LB Phoenix

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3106);

        //Deadly Embrace
        row = summonmodeTable.GetRow(2);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3100);

        //Blind Justice
        row = summonmodeTable.GetRow(4);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3101);

        // //Wings of Light
        // row = summonmodeTable.GetRow(8);

        // row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3103);
        
        //Cold Snap
        row = summonmodeTable.GetRow(5);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3104);

        //Arm of Darkness
        row = summonmodeTable.GetRow(7);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3105);
        
        //Serpent's Cry
        row = summonmodeTable.GetRow(9);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3107);

        //Do Luminous Advent
        row = summonmodeTable.GetRow(10);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3103);
        
        //PLAYERCOMMANDBUILDER

        //Do Logos Zantetsuken
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3131);

        //Jump
        column = "Unk20";

        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            if (i == 32)
                continue;   //This is to exclude chocobo mode, otherwise player can't sprint-jump while mounted

            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            else if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 4) 
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3118);
        }

        //Lunge
        column = "Unk14";

        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;
            
            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3 || row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3123) 
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3117);
        }

        //Dodge
        column = "Unk8";

        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            if (i == 23)    //Prevent Wings of Light dodge from being affected
                continue;

            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 11)
            {
                if (i == 38)    //Do Logos Titanic Block
                    row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 236);
                else
                    row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3120);
            }
        }

        // //Do Logos Cold Snap
        // column = "Unk7";

        // row = playercommandbuilderTable!.GetRow(playercommandbuilderTable.GetMainKeyByIndex(38), 0);

        // row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 261);


        //Do Wings of Flight dodge distance increase
        row = systemmoveTable!.GetRow(1001)!;

        row.SetSingle((uint)systemmoveLayout.Columns["Unk2"].Offset, 6.0f);
    }

    private void UndoSpecialAttack()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        //SUMMONMODE
        string column = "CommandId";

        //Phoenix Shift
        INexRow? row = summonmodeTable!.GetRow(1);   //Post LB Phoenix

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 201);

        row = summonmodeTable.GetRow(6);    //Pre LB Phoenix

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 201);

        //Deadly Embrace
        row = summonmodeTable.GetRow(2);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 222);

        //Blind Justice
        row = summonmodeTable.GetRow(4);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 321);

        // //Wings of Light
        // row = summonmodeTable.GetRow(8);

        // row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 301);
        
        //Cold Snap
        row = summonmodeTable.GetRow(5);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 261);

        //Arm of Darkness
        row = summonmodeTable.GetRow(7);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 281);
        
        //Serpent's Cry
        row = summonmodeTable.GetRow(9);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 341);

        //Undo Luminous Advent
        row = summonmodeTable.GetRow(10);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3108);
        
        //PLAYERCOMMANDBUILDER

        //Undo Logos Zantetsuken
        column = "Unk7";

        row = playercommandbuilderTable!.GetRow(38, 0);

        row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3121);

        //Jump
        column = "Unk20";

        for (uint i = 0; i < playercommandbuilderTable!.GetNumRows(); i++)
        {
            if (i == 32)
                continue;   //This is to exclude chocobo mode, otherwise player can't sprint-jump while mounted

            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3118) 
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 4);
        }

        //Lunge
        column = "Unk14";

        for (uint i = 0; i < playercommandbuilderTable.GetNumRows(); i++)
        {
            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;

            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3117)
                if (i == 38)
                    row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3123);
                else 
                    row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 3);
        }

        //Dodge
        column = "Unk8";

        for (uint i = 0; i < playercommandbuilderTable.GetNumRows(); i++)
        {
            row = playercommandbuilderTable.GetRow(playercommandbuilderTable.GetMainKeyByIndex(i), 0);

            if (row is null)
                continue;
            
            if (i == 23)    //Prevent Wings of Light dodge from being affected
                continue;
                
            if (row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 3120 || row!.GetInt32((uint)playercommandbuilderLayout.Columns[column].Offset) == 236) 
                row.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 11);
        }

        // //Undo Logos Cold Snap
        // column = "Unk7";

        // row = playercommandbuilderTable!.GetRow(playercommandbuilderTable.GetMainKeyByIndex(38), 0);

        // row!.SetInt32((uint)playercommandbuilderLayout.Columns[column].Offset, 281);

        //Undo Wings of Flight dodge distance increase
        row = systemmoveTable!.GetRow(1001)!;

        row.SetSingle((uint)systemmoveLayout.Columns["Unk2"].Offset, 2.0f);
    }

    private void DoComboFinisher()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;
        
        isComboFinisherActive = true;

        int[] groundBase = new int[] { _configuration.attack1, _configuration.attack2, _configuration.attack3, _configuration.attack4 };
        // int[] groundBaseBurst = new int[] { _configuration.magicburst1, _configuration.magicburst2, _configuration.magicburst3, _configuration.magicburst4 };
        int[] airBase = new int[] { _configuration.airattack1, _configuration.airattack2, _configuration.airattack3 };
        // int[] airBaseBurst = new int[] { _configuration.airmagicburst1, _configuration.airmagicburst2, _configuration.airmagicburst3 };

        // int[] groundLB = new int[] { _configuration.attackLB1, _configuration.attackLB2, _configuration.attackLB3, _configuration.attackLB4 };
        // int[] groundLBBurst = new int[] { _configuration.magicburstLB1, _configuration.magicburstLB2, _configuration.magicburstLB3, _configuration.magicburstLB4 };
        // int[] airLB = new int[] { _configuration.airattackLB1, _configuration.airattackLB2, _configuration.airattackLB3 };
        // int[] airLBBurst = new int[] { _configuration.airmagicburstLB1, _configuration.airmagicburstLB2, _configuration.airmagicburstLB3 };

        // int[] groundBaseVanilla = new int[] { _configuration.attack1Base, _configuration.attack2Base, _configuration.attack3Base, _configuration.attack4Base };
        // int[] groundBaseBurstVanilla = new int[] { 199, 200, 201, 202 };
        // int[] airBaseVanilla = new int[] { _configuration.airattack1Base, _configuration.airattack2Base, _configuration.airattack3Base };
        // int[] airBaseBurstVanilla = new int[] { 203, 204, 205 };

        // int[] groundLBVanilla = new int[] { 316, 317, 318, 319 };
        // int[] groundLBBurstVanilla = new int[] { 199, 200, 201, 202 };
        // int[] airLBVanilla = new int[] { 320, 321, 322 };
        // int[] airLBBurstVanilla = new int[] { 203, 204, 205 };

        int[] groundLogos = new int[] { _configuration.attackLogosFinisher1, _configuration.attackLogosFinisher2, _configuration.attackLogosFinisher3, _configuration.attackLogosFinisher4 };
        int[] airLogos = new int[] { _configuration.airattackLogosFinisher1, _configuration.airattackLogosFinisher2, _configuration.airattackLogosFinisher3 };

        string column;
        INexRow? row;

        for (uint i = 0; i < 4; i++)
        {
            row = comboTable!.GetRow(1, i)!;    //Key1 1 is adult clive base combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBase[i]);
            
            // column = "Unk8";

            // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == groundBaseBurstVanilla[i])
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseBurst[i]);
            // else
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseBurstVanilla[i]);

            if (i < 3)  //Check to prevent index going outside array range for air combo
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airBase[i]);

                // column = "Unk9";

                // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == airBaseBurstVanilla[i])
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseBurst[i]);
                // else
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseBurstVanilla[i]);    
            }    

            row = comboTable!.GetRow(30, i)!;    //Key1 30 is Logos combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogos[i]);
            
            // column = "Unk8";

            // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == groundBaseBurstVanilla[i])
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseBurst[i]);
            // else
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseBurstVanilla[i]);

            if (i < 3)  //Check to prevent index going outside array range for air combo
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogos[i]);

                // column = "Unk9";

                // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == airBaseBurstVanilla[i])
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseBurst[i]);
                // else
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseBurstVanilla[i]);    
            }    

            row = comboTable!.GetRow(5, i)!;   //Key1 5 is LB combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBase[i]);
            
            // column = "Unk8";

            // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == groundLBBurstVanilla[i])
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLBBurst[i]);
            // else
            //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLBBurstVanilla[i]);

            if (i < 3)  //Check to prevent index going outside array range for air combo
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airBase[i]);

                // column = "Unk9";

                // if (row!.GetInt32((uint)comboLayout.Columns[column].Offset) == airLBBurstVanilla[i])
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airLBBurst[i]);
                // else
                //     row.SetInt32((uint)comboLayout.Columns[column].Offset, airLBBurstVanilla[i]);       
            }     
        }

        // Override eikonic abilities
        row = playercommandbuilderTable!.GetRow(38, 0)!;

        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk16"].Offset, 3129);
        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk17"].Offset, 3130);

        // Do parry counter
        row = comboTable!.GetRow(1, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 191);

        row = comboTable!.GetRow(5, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 191);
    }

    private void UndoComboFinisher()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;
        
        isComboFinisherActive = false;

        // int[] groundBase = new int[] { _configuration.attack1, _configuration.attack2, _configuration.attack3, _configuration.attack4 };
        // int[] groundBaseBurst = new int[] { _configuration.magicburst1, _configuration.magicburst2, _configuration.magicburst3, _configuration.magicburst4 };
        // int[] airBase = new int[] { _configuration.airattack1, _configuration.airattack2, _configuration.airattack3 };
        // int[] airBaseBurst = new int[] { _configuration.airmagicburst1, _configuration.airmagicburst2, _configuration.airmagicburst3 };

        // int[] groundLB = new int[] { _configuration.attackLB1, _configuration.attackLB2, _configuration.attackLB3, _configuration.attackLB4 };
        // int[] groundLBBurst = new int[] { _configuration.magicburstLB1, _configuration.magicburstLB2, _configuration.magicburstLB3, _configuration.magicburstLB4 };
        // int[] airLB = new int[] { _configuration.airattackLB1, _configuration.airattackLB2, _configuration.airattackLB3 };
        // int[] airLBBurst = new int[] { _configuration.airmagicburstLB1, _configuration.airmagicburstLB2, _configuration.airmagicburstLB3 };

        int[] groundBaseVanilla = new int[] { _configuration.attack1Base, _configuration.attack2Base, _configuration.attack3Base, _configuration.attack4Base };
        // int[] groundBaseBurstVanilla = new int[] { 199, 200, 201, 202 };
        int[] airBaseVanilla = new int[] { _configuration.airattack1Base, _configuration.airattack2Base, _configuration.airattack3Base };
        // int[] airBaseBurstVanilla = new int[] { 203, 204, 205 };

        int[] groundLBVanilla = new int[] { _configuration.attackLB1, _configuration.attackLB2, _configuration.attackLB3, _configuration.attackLB4 };
        // int[] groundLBBurstVanilla = new int[] { 199, 200, 201, 202 };
        int[] airLBVanilla = new int[] { _configuration.airattackLB1, _configuration.airattackLB2, _configuration.airattackLB3 };
        // int[] airLBBurstVanilla = new int[] { 203, 204, 205 };

        string column;
        INexRow? row;

        for (uint i = 0; i < 4; i++)
        {
            row = comboTable!.GetRow(1, i)!;    //Key1 1 is adult clive base combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseVanilla[i]);

            if (i < 3)  //Check to prevent index going outside array range for air combo
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseVanilla[i]);
            }    

            row = comboTable!.GetRow(30, i)!;    //Key1 30 is Logos combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLogosVanilla[i]);

            if (i < 3)
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airLogosVanilla[i]);
            }    

            row = comboTable!.GetRow(5, i)!;   //Key1 5 is LB combo

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLBVanilla[i]);

            if (i < 3)  //Check to prevent index going outside array range for air combo
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airLBVanilla[i]);
            }     
        }

        // Override Logos abilities
        row = playercommandbuilderTable!.GetRow(38, 0)!;

        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk16"].Offset, 0);
        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk17"].Offset, 0);

        // Undo parry counter
        row = comboTable!.GetRow(1, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 0);

        row = comboTable!.GetRow(5, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 0);
    }

    private void ReplaceComboStrings()
    {
        if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
            return;
        }

        if (!nextExcelDBApi.Initialized)
            return;

        int[] groundBaseCombo = new int[] { _configuration.attack1Base, _configuration.attack2Base, _configuration.attack3Base, _configuration.attack4Base };
        int[] airBaseCombo = new int[] { _configuration.airattack1Base, _configuration.airattack2Base, _configuration.airattack3Base };
        int[] groundLBCombo = new int[] { _configuration.attackLB1, _configuration.attackLB2, _configuration.attackLB3, _configuration.attackLB4 };
        int[] airLBCombo = new int[] { _configuration.airattackLB1, _configuration.airattackLB2, _configuration.airattackLB3 };

        string column;

        for (uint i = 0; i < 4; i++)
        {
            INexRow? row = comboTable!.GetRow(1, i)!;

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseCombo[i]);

            if (i < 3)
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseCombo[i]);
            }
        }

        // for (uint i = 0; i < 4; i++)
        // {
        //     INexRow? row = comboTable!.GetRow(30, i)!;

        //     column = "Unk2";

        //     row.SetInt32((uint)comboLayout.Columns[column].Offset, groundBaseCombo[i]);

        //     if (i < 3)
        //     {
        //         column = "Unk5";

        //         row.SetInt32((uint)comboLayout.Columns[column].Offset, airBaseCombo[i]);
        //     }
        // }

        for (uint i = 0; i < 4; i++)
        {
            INexRow? row = comboTable!.GetRow(5, i)!;

            column = "Unk2";

            row.SetInt32((uint)comboLayout.Columns[column].Offset, groundLBCombo[i]);

            if (i < 3)
            {
                column = "Unk5";

                row.SetInt32((uint)comboLayout.Columns[column].Offset, airLBCombo[i]);
            }
        }
    }

    // private void SwitchStyle()
    // {
    //     if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
    //     {
    //         _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
    //         return;
    //     }

    //     if (!nextExcelDBApi.Initialized)
    //         return;

    //     INexRow? row = weaponbaseTable!.GetRow(101)!;

    //     string column = "MotionWeaponTypeId";

    //     int wepType = row.GetInt32((uint)weaponbaseLayout.Columns[column].Offset);

    //     Console.WriteLine(wepType);

    //     if (wepType == 2)
    //         row.SetInt32((uint)weaponbaseLayout.Columns[column].Offset, 7);
    //     else
    //         row.SetInt32((uint)weaponbaseLayout.Columns[column].Offset, 2);
    // }

    private void DoFlyUp()
    {
        // if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        // {
        //     _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
        //     return;
        // }

        // if (!nextExcelDBApi.Initialized)
        //     return;
        
        // INexRow? row = systemmoveTable!.GetRow(70)!;

        // row.SetSingle((uint)systemmoveLayout.Columns["Unk6"].Offset, 2.0f);
    }

    private void UndoFlyUp()
    {
        // if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        // {
        //     _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
        //     return;
        // }

        // if (!nextExcelDBApi.Initialized)
        //     return;
        
        // INexRow? row = systemmoveTable!.GetRow(70)!;
        
        // row.SetSingle((uint)systemmoveLayout.Columns["Unk6"].Offset, 0.0f);
    }

    private void DoFlyDown()
    {
        // if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        // {
        //     _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
        //     return;
        // }

        // if (!nextExcelDBApi.Initialized)
        //     return;
        
        // INexRow? row = systemmoveTable!.GetRow(70)!;

        // row.SetSingle((uint)systemmoveLayout.Columns["Unk6"].Offset, -2.0f);

        //Do Heavensward -> Earthbound
        INexRow? row = playercommandbuilderTable!.GetRow(23)!;

        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 3133);
    }
    
    private void UndoFlyDown()
    {
        // if (!_nexApi!.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        // {
        //     _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?");
        //     return;
        // }

        // if (!nextExcelDBApi.Initialized)
        //     return;
        
        // INexRow? row = systemmoveTable!.GetRow(70)!;

        // row.SetSingle((uint)systemmoveLayout.Columns["Unk6"].Offset, 0.0f);

        //Undo Heavensward -> Earthbound
        INexRow? row = playercommandbuilderTable!.GetRow(23)!;

        row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 302);
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");

        ReplaceComboStrings();
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}