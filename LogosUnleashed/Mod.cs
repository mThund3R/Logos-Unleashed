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
using FF16Framework.Interfaces.Nex.Structures;

using System.Diagnostics;

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
    private Extras _extras;

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
    public static NexTableLayout uiarrayLayout = TableMappingReader.ReadTableLayout("uiarray", new Version(1, 0, 0));

    public static INexTable? summonmodeTable;
    public static INexTable? playercommandbuilderTable;
    public static INexTable? actionTable;
    public static INexTable? comboTable;
    // public static INexTable? weaponbaseTable;
    public static INexTable? systemmoveTable;
    public static INexTable? uiarrayTable;
    
    public static bool isComboFinisherActive = false;

    public KeyInterceptor? interceptor;

    public static Thread? keyboardInputThread;
    public static Thread? controllerInputThread;
    public static Thread? dualSenseInputThread;

    public static int[] groundLogosVanilla = { 139, 358, 5104, 1162 };
    public static int[] airLogosVanilla = { 297, 298, 299};
    public static int[] groundLogosSpin = { 143, 143, 183, 183 };
    public static int[] groundLogosStinger = { 4996, 4996, 166, 4660 };

    public static int[] groundLogosBurstVanilla = { 199, 200, 201, 202 };
    public static int[] airLogosBurstVanilla = { 424, 424, 424 };
    public static int[] groundLogosBurstLauncher = { 175, 175, 175, 175 };
    public static int[] airLogosBurstLauncher = { 463, 463, 463 };
    public static int[] airLogosBurstGlide = { 871, 871, 871 };

    public static int[] groundLogosVanillaPostburst = [0, 154, 155, 296];
    public static int[] groundLogosAlternatePostburst = [0, 0, 0, 0];

    //public static bool isSkillTreeChanged = false;

    public static Process thisProcess = Process.GetCurrentProcess();

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _extras = context.Extras;
        _modConfig = context.ModConfig;
        
        _logger.WriteLine($"[{_modConfig.ModId}] Initializing..", _logger.ColorGreen);

        _nexApi = _modLoader.GetController<INextExcelDBApiManaged>();
        if (_nexApi is null || !_nexApi.TryGetTarget(out INextExcelDBApiManaged? nextExcelDBApi))
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Could not get INextExcelDBApi. Is the FFXVI Mod Framework installed/loaded?", _logger.ColorRed);
            return;
        }

        nextExcelDBApi.OnNexLoaded += NextExcelDBApi_OnNexLoaded;

        keyboardInputThread = new Thread(KeyboardInputThread);
        controllerInputThread = new Thread(ControllerInputThread);
        dualSenseInputThread = new Thread(DualSenseInputThread);
    }

    /// <summary>
    /// Fired when the game has loaded all nex tables.
    /// </summary>
    private void NextExcelDBApi_OnNexLoaded()
    {
        //SaveSerialization();

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
        uiarrayTable = nextExcelDBApi.GetTable(NexTableIds.uiarray);

        if (_configuration.isKeyboardEnabled)
        {
            // _logger.WriteLine($"[{_modConfig.ModId}] Keyboard input has been enabled. Send your feedback on the nexus!", _logger.ColorYellow);
            keyboardInputThread!.IsBackground = true;
            keyboardInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] Keyboard support is disabled. You can enable it in mod configuration menu.", _logger.ColorYellow);

        if (_configuration.isControllerEnabled)
        {
            controllerInputThread!.IsBackground = true;
            controllerInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] XInput Gamepad support has been disabled. You can re-enable it in mod configuration menu.", _logger.ColorYellow);

        if (_configuration.isDualSenseEnabled)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Experimental DualSense input has been enabled. Send your feedback on the Nexus!", _logger.ColorYellow);
            dualSenseInputThread!.IsBackground = true;
            dualSenseInputThread.Start();
        }
        else
            _logger.WriteLine($"[{_modConfig.ModId}] DualSense support is disabled. You can enable it in mod configuration menu.", _logger.ColorYellow);
        
        if (!_configuration.isKeyboardEnabled && !_configuration.isControllerEnabled && !_configuration.isDualSenseEnabled)
        {
            _logger.WriteLine($"[{_modConfig.ModId}] Support for all input devices is disabled. Mod will not work properly.", _logger.ColorRed);
        }

        // ReplaceComboStrings();
        //UndoComboFinisher();    //To apply new user-defined combo strings
    }

    //private const uint PAGE_EXECUTE_READWRITE = 0x40;
    //private const nuint SerializeSaveOverrideOffset = 0x79711D;

    //public unsafe void SaveSerialization()
    //{
    //    //Scanner scanner = new Scanner((byte*)thisProcess.MainModule!.BaseAddress, thisProcess.MainModule.ModuleMemorySize);

    //    //var offset = scanner.FindPattern_Simple("48 89 5C 24 ? 55 56 57 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 84 24 ? ? ? ? 48 89 91");

    //    nuint address = (nuint)thisProcess.MainModule!.BaseAddress + SerializeSaveOverrideOffset;

    //    Console.WriteLine(address);

    //    Memory.Instance.ChangeProtectionRaw(address, 2, PAGE_EXECUTE_READWRITE);

    //    byte[] getByte = Memory.Instance.ReadRaw(address, 1);
    //    Console.WriteLine(getByte[0].ToString());

    //    if (getByte[0] == 0x74)
    //    {
    //        Marshal.Copy((byte[])[0x75], 0, (nint)address, 1);

    //        Console.WriteLine("SerializeSave Changed");
    //    }
    //    else Console.WriteLine("SerializeSave NOT Changed");
    //}

    private void KeyboardShortcutMake()
    {
        Shortcut[] shortcuts =
        [
            new Shortcut(Key.W, KeyModifier.None, state: KeyState.Down, name: "W Down"),
            new Shortcut(Key.S, KeyModifier.None, state: KeyState.Down, name: "S Down"),
            new Shortcut(_configuration.specialAttackKeybindKeyboard, KeyModifier.None, state: KeyState.Down, name: "CapsLock Down"),
            new Shortcut(Key.W, KeyModifier.None, state: KeyState.Up, name: "W Up"),
            new Shortcut(Key.S, KeyModifier.None, state: KeyState.Up, name: "S Up"),
            new Shortcut(_configuration.specialAttackKeybindKeyboard, KeyModifier.None, state: KeyState.Up, name: "CapsLock Up"),

            new Shortcut(_configuration.alternateComboKeybindKeyboard, KeyModifier.None, state: KeyState.Down, name: "X Down"),
            new Shortcut(_configuration.alternateComboKeybindKeyboard, KeyModifier.None, state: KeyState.Up, name: "X Up"),

            // new Shortcut(_configuration.flyUpKeybindKeyboard, state: KeyState.Down, name: "FlyUp Down"),
            // new Shortcut(_configuration.flyUpKeybindKeyboard, state: KeyState.Up, name: "FlyUp Up"),
            // new Shortcut(_configuration.flyDownKeybindKeyboard, state: KeyState.Down, name: "FlyDown Down"),
            // new Shortcut(_configuration.flyDownKeybindKeyboard, state: KeyState.Up, name: "FlyDown Up"),
            
            new Shortcut(_configuration.directionalHoldKeybindKeyboard, KeyModifier.None, state: KeyState.Down, name: "Directional Down"),
            new Shortcut(_configuration.directionalHoldKeybindKeyboard, KeyModifier.None, state: KeyState.Up, name: "Directional Up"),

            //new Shortcut(_configuration.changeSkillTreeKeybindKeyboard, KeyModifier.None, state: KeyState.Down, name: "ChangeSkillTree Down")
        ];

        interceptor = new KeyInterceptor(shortcuts);
    }

    private void KeyboardInputThread()
    {
        KeyboardShortcutMake();

        bool wDown = false;
        bool sDown = false;
        bool clDown = false;

        bool directionalDown;
        if (_configuration.isDirectionalButtonHold)
            directionalDown = false;
        else
            directionalDown = true;

        interceptor!.ShortcutPressed += (_, e) =>
        {
            switch (e.Shortcut.Name)
            {
                case "W Down":
                    wDown = true;
                    if (!clDown && !sDown && directionalDown) DoUpAttack();
                break;
                case "S Down":
                    sDown = true;
                    if (!clDown && !wDown && directionalDown) DoDownAttack();
                break;
                case "CapsLock Down":
                    clDown = true;
                    if (wDown) UndoUpAttack();
                    if (sDown) UndoDownAttack();
                    DoSpecialAttack();
                break;
                case "W Up":
                    wDown = false;
                    UndoUpAttack();
                    if (clDown) DoSpecialAttack();
                    else if (sDown) DoDownAttack();
                break;
                case "S Up":
                    sDown = false;
                    UndoDownAttack();
                    if (clDown) DoSpecialAttack();
                    else if (wDown) DoUpAttack();
                break;
                case "CapsLock Up":
                    clDown = false;
                    UndoSpecialAttack();
                    if (sDown) DoDownAttack();
                    if (wDown) DoUpAttack();
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
                // case "FlyDown Down":
                //     DoFlyDown();
                // break;
                // case "FlyDown Up":
                //     UndoFlyDown();
                // break;

                case "Directional Down":
                    if (_configuration.isDirectionalButtonHold)
                    {
                        directionalDown = true;
                    }
                break;
                case "Directional Up":
                    if (_configuration.isDirectionalButtonHold)
                    {
                        directionalDown = false;
                        if (!clDown)
                        {
                            UndoUpAttack();
                            UndoDownAttack();
                        }
                    }
                break;

                //case "ChangeSkillTree Down":
                //    if (!isSkillTreeChanged)
                //    {
                //        DoChangeSkillTree();
                //        isSkillTreeChanged = true;
                //    }
                //    else
                //    {
                //        UndoChangeSkillTree();
                //        isSkillTreeChanged = false;
                //    }
                //break;
            }
        };

        interceptor.RunMessageLoop();
    }

    private void ControllerInputThread()
    {
        GamepadButtonFlags previousButtons = 0;
        byte previousLeftTrigger = 0;
        byte previousRightTrigger = 0;
        int previousLeftStickY = 0;

        var isTriggerHeld = false;
        var isDirectionalHeld = false;

        while(controller.IsConnected && _configuration.isControllerEnabled)
        {
            var state = controller.GetState();
            GamepadButtonFlags currentButtons = state.Gamepad.Buttons;

            int deadZoneVertical = _configuration.deadZoneVertical;
            int deadZoneR2 = _configuration.deadZoneR2;
            GamepadButtonFlags specialAttackKeybind = (GamepadButtonFlags)_configuration.specialAttackKeybind;
            GamepadButtonFlags alternateComboKeybind = (GamepadButtonFlags)_configuration.alternateComboKeybind;

            // GamepadButtonFlags flyUpKeybind = (GamepadButtonFlags)_configuration.flyUpKeybindController;
            // GamepadButtonFlags flyDownKeybind = (GamepadButtonFlags)_configuration.flyDownKeybindController;

            GamepadButtonFlags directionalHoldKeybind = (GamepadButtonFlags)_configuration.directionalHoldKeybind;

            int currentLeftStickY = state.Gamepad.LeftThumbY;
            byte currentLeftTrigger = state.Gamepad.LeftTrigger;
            byte currentRightTrigger = state.Gamepad.RightTrigger;
            
            if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger)
            {
                if (currentLeftTrigger > deadZoneR2)
                {
                    DoSpecialAttack();
                    isTriggerHeld = true;
                }
                else if (currentLeftTrigger <= deadZoneR2 && previousLeftTrigger > deadZoneR2)
                {
                    UndoSpecialAttack();
                    isTriggerHeld = false;
                }
            }

            if (_configuration.specialAttackKeybind == ButtonTriggerFlags.RightTrigger)
            {
                if (currentRightTrigger > deadZoneR2)
                {
                    DoSpecialAttack();
                    isTriggerHeld = true;
                }
                else if (currentRightTrigger <= deadZoneR2 && previousRightTrigger > deadZoneR2)
                {
                    UndoSpecialAttack();
                    isTriggerHeld = false;
                }
            }

            if (_configuration.specialAttackKeybind != ButtonTriggerFlags.RightTrigger && _configuration.specialAttackKeybind != ButtonTriggerFlags.LeftTrigger)
            {
                if ((currentButtons & specialAttackKeybind) != 0)
                {
                    DoSpecialAttack();
                    isTriggerHeld = true;
                }
                else if ((currentButtons & specialAttackKeybind) == 0 && (previousButtons & specialAttackKeybind) != 0)
                {
                    UndoSpecialAttack();
                    isTriggerHeld = false;
                }
            }

            if (_configuration.isDirectionalButtonHold)
            {
                if ((currentButtons & directionalHoldKeybind) != 0 && (previousButtons & directionalHoldKeybind) == 0)
                {
                    isDirectionalHeld = true;
                }
                else if ((currentButtons & directionalHoldKeybind) == 0 && (previousButtons & directionalHoldKeybind) != 0)
                {
                    if (!isTriggerHeld)
                    {
                        UndoUpAttack();
                        UndoDownAttack();
                    }

                    isDirectionalHeld = false;
                }
            }
            else
            {
                isDirectionalHeld = true;
            }

            if (currentLeftStickY > deadZoneVertical && !isTriggerHeld && isDirectionalHeld)
            {
                DoUpAttack();
            }
            else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY > deadZoneVertical)
            {
                UndoUpAttack();
            }
            
            if (currentLeftStickY < deadZoneVertical * (-1) && !isTriggerHeld && isDirectionalHeld)
            {
                DoDownAttack();
            }
            else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1))
            {
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

            // if ((currentButtons & flyDownKeybind) != 0 && (previousButtons & flyDownKeybind) == 0)
            // {
            //     DoFlyDown();
            // }
            // if ((currentButtons & flyDownKeybind) == 0 && (previousButtons & flyDownKeybind) != 0)
            // {
            //     UndoFlyDown();
            // }

            //if ((currentButtons & GamepadButtonFlags.RightShoulder) != 0 && (currentButtons & GamepadButtonFlags.LeftShoulder) != 0 && ((previousButtons & GamepadButtonFlags.RightShoulder) == 0 || (previousButtons & GamepadButtonFlags.LeftShoulder) == 0))
            //{
            //    if (!isSkillTreeChanged)
            //    {
            //        //Console.WriteLine("pressed");
            //        DoChangeSkillTree();
            //        isSkillTreeChanged = true;
            //    }
            //    else
            //    {
            //        UndoChangeSkillTree();
            //        isSkillTreeChanged = false;
            //    }
            //}
            
            previousButtons = currentButtons;
            previousLeftTrigger = currentLeftTrigger;
            previousRightTrigger = currentRightTrigger;
            previousLeftStickY = currentLeftStickY;
        }

        _logger.WriteLine($"[{_modConfig.ModId}] XInput Gamepad not found. Is it compatible with XInput?", _logger.ColorYellow);
        _logger.WriteLine($"[{_modConfig.ModId}] After connecting your controller, open and close the mod configuration menu to try again", _logger.ColorYellow);
    }

    private void DualSenseInputThread()
    {
        bool previousSpecialButton = false;
        bool previousFinisherButton = false;
        bool previousDirectionalHoldButton = false;
        //bool previousChangeSkillTreeButton1 = false;
        //bool previousChangeSkillTreeButton2 = false;
        float previousLeftTrigger = 0.0f;
        float previousRightTrigger = 0.0f;
        float previousLeftStickY = 0.0f;

        if (DualSense.EnumerateControllers().Count() > 0)
        {
            dualSense = DualSense.EnumerateControllers().First();
            dualSense.Acquire();
    
            float deadZoneVertical = _configuration.deadZoneVerticalDualSense;
            float deadZoneR2 = _configuration.deadZoneR2DualSense;

            bool isTriggerHeld = false;
            bool isDirectionalHeld = false;
    
            dualSense.OnStatePolled += (sender) =>
            {
                var state = sender.InputState;
                float currentLeftStickY = state.LeftAnalogStick.Y;
    
                bool[] currentButtons = GetDualSenseKeys(state);
                bool currentSpecialButton = currentButtons[0];
                bool currentFinisherButton = currentButtons[1];
                bool currentDirectionalHoldButton = currentButtons[2];

                //bool currentChangeSkillTreeButton1 = dualSense.InputState.L1Button;
                //bool currentChangeSkillTreeButton2 = dualSense.InputState.R1Button;
                
                float currentLeftTrigger = state.L2;
                float currentRightTrigger = state.R2;
    
                if (_configuration.specialAttackKeybind == ButtonTriggerFlags.LeftTrigger)
                {
                    if (currentLeftTrigger > deadZoneR2 && previousLeftTrigger <= deadZoneR2)
                    {
                        DoSpecialAttack();
                        isTriggerHeld = true;
                    }
                    else if (currentLeftTrigger <= deadZoneR2 && previousLeftTrigger > deadZoneR2)
                    {
                        UndoSpecialAttack();
                        isTriggerHeld = false;
                    }
                }
    
                if (_configuration.specialAttackKeybind == ButtonTriggerFlags.RightTrigger)
                {
                    if (currentRightTrigger > deadZoneR2 && previousRightTrigger <= deadZoneR2)
                    {
                        DoSpecialAttack();
                        isTriggerHeld = true;
                    }
                    else if (currentRightTrigger <= deadZoneR2 && previousRightTrigger > deadZoneR2)
                    {
                        UndoSpecialAttack();
                        isTriggerHeld = false;
                    }
                }
                
                if (_configuration.specialAttackKeybind != ButtonTriggerFlags.LeftTrigger && _configuration.specialAttackKeybind != ButtonTriggerFlags.RightTrigger)
                {
                    if (currentSpecialButton && !previousSpecialButton)
                    {
                        DoSpecialAttack();
                        isTriggerHeld = true;
                    }
                    else if (!currentSpecialButton && previousSpecialButton)
                    {
                        UndoSpecialAttack();
                        isTriggerHeld = false;
                    }    
                }
    
                if (_configuration.isDirectionalButtonHold)
                {
                    if (currentDirectionalHoldButton && !previousDirectionalHoldButton)
                    {
                        isDirectionalHeld = true;
                    }
                    else if (!currentDirectionalHoldButton && previousDirectionalHoldButton)
                    {
                        if (!isTriggerHeld)
                        {
                            UndoUpAttack();
                            UndoDownAttack();
                        }
    
                        isDirectionalHeld = false;
                    }
                }
                else
                {
                    isDirectionalHeld = true;
                }
    
                if (currentLeftStickY > deadZoneVertical && !isTriggerHeld && isDirectionalHeld)
                {
                    DoUpAttack();
                }
                else if (currentLeftStickY <= deadZoneVertical && previousLeftStickY >= deadZoneVertical && !isTriggerHeld)
                {
                    UndoUpAttack();
                }
                
                if (currentLeftStickY < deadZoneVertical * (-1) && !isTriggerHeld && isDirectionalHeld)
                {
                    DoDownAttack();
                }
                else if (currentLeftStickY >= deadZoneVertical * (-1) && previousLeftStickY < deadZoneVertical * (-1) && !isTriggerHeld)
                {
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

                //if (currentChangeSkillTreeButton1 && currentChangeSkillTreeButton2 && (!previousChangeSkillTreeButton1 || !previousChangeSkillTreeButton2))
                //{
                //    if (!isSkillTreeChanged)
                //    {
                //        DoChangeSkillTree();
                //        isSkillTreeChanged = true;
                //    }
                //    else
                //    {
                //        UndoChangeSkillTree();
                //        isSkillTreeChanged = false;
                //    }
                //}

                previousSpecialButton = currentSpecialButton;
                previousFinisherButton = currentFinisherButton;
                previousLeftStickY = currentLeftStickY;
                previousDirectionalHoldButton = currentDirectionalHoldButton;
                previousLeftTrigger = currentLeftTrigger;
                previousRightTrigger = currentRightTrigger;
                //previousChangeSkillTreeButton1 = currentChangeSkillTreeButton1;
                //previousChangeSkillTreeButton2 = currentChangeSkillTreeButton2;
            };
        }

        if (DualSense.EnumerateControllers().Count() > 0)
        {
            dualSense!.BeginPolling(_configuration.dualSensePollingRate);
            while (DualSense.EnumerateControllers().Count() > 0 && _configuration.isDualSenseEnabled)
            {
                //do nothing
            }
            dualSense.EndPolling();
            dualSense.Release();
        }
        
        _logger.WriteLine($"[{_modConfig.ModId}] DualSense not found", _logger.ColorYellow);
        _logger.WriteLine($"[{_modConfig.ModId}] After connecting your controller, open and close the mod configuration menu to try again", _logger.ColorYellow);
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

        //Do Odin Dark Glide
        column = "Unk5";

        for (uint i = 0; i < 5; i++)
        {
            row = comboTable!.GetRow(2, i)!;

            if (i == 0) row.SetInt32((uint)comboLayout.Columns[column].Offset, 871);
            else row.SetInt32((uint)comboLayout.Columns[column].Offset, 0);
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

        //Undo Odin Dark Glide
        column = "Unk5";

        for (uint i = 0; i < 5; i++)
        {
            row = comboTable!.GetRow(2, i)!;

            row.SetInt32((uint)comboLayout.Columns[column].Offset, row.GetInt32((uint)comboLayout.Columns["Unk2"].Offset));
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

        //Titanic Block
        row = summonmodeTable.GetRow(3);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 3102);
        
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
        //row = systemmoveTable!.GetRow(1001)!;

        //row.SetSingle((uint)systemmoveLayout.Columns["Unk2"].Offset, 6.0f);

        //Do Heavensward -> Luminous Advent
        row = playercommandbuilderTable!.GetRow(23, 0)!;

        if (row.GetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset) == 302) row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 3153);

        //Do Wings of Flight dodge -> Luminous Advent
        //row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk8"].Offset, 3153);
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

        //Titanic Block
        row = summonmodeTable.GetRow(3);

        row!.SetInt32((uint)summonmodeLayout.Columns[column].Offset, 236);
        
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
        //row = systemmoveTable!.GetRow(1001)!;

        //row.SetSingle((uint)systemmoveLayout.Columns["Unk2"].Offset, 2.0f);

        //Undo Heavensward -> Luminous Advent
        row = playercommandbuilderTable!.GetRow(23, 0)!;

        if (row.GetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset) == 3153) row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 302);

        //Undo Wings of Flight dodge -> Luminous Advent
        //row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk8"].Offset, 11);
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

        int[] groundBase = new int[] { _extras.attack1, _extras.attack2, _extras.attack3, _extras.attack4 };
        int[] airBase = new int[] { _extras.airattack1, _extras.airattack2, _extras.airattack3 };

        int[] groundLogos = new int[] { _extras.attackLogosFinisher1, _extras.attackLogosFinisher2, _extras.attackLogosFinisher3, _extras.attackLogosFinisher4 };
        int[] airLogos = new int[] { _extras.airattackLogosFinisher1, _extras.airattackLogosFinisher2, _extras.airattackLogosFinisher3 };

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

            if (i == 0)
            {
                row.SetInt32((uint)comboLayout.Columns["Unk20"].Offset, groundBase[i]);
                row.SetInt32((uint)comboLayout.Columns["Unk12"].Offset, groundBase[i]);
                row.SetInt32((uint)comboLayout.Columns["Unk14"].Offset, groundBase[i]);
                row.SetInt32((uint)comboLayout.Columns["Unk15"].Offset, groundBase[i]);
                row.SetInt32((uint)comboLayout.Columns["Unk16"].Offset, groundBase[i]);
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

            row = comboTable!.GetRow(24, i)!;   //Key1 24 is Berserker combo

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

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 4655);

        row = comboTable!.GetRow(5, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 4655);

        row = comboTable!.GetRow(30, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 4655);

        row = comboTable!.GetRow(24, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 4655);

        //SUMMONMODE
        //Ultima
        row = summonmodeTable!.GetRow(10)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 1599);  //Do Aether Ray
        row.SetInt32((uint)summonmodeLayout.Columns["Unk29"].Offset, 1722); //Do Aether Lance

        //Ramuh
        row = summonmodeTable.GetRow(4)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 674);   //Do Blind Exact
        row.SetInt32((uint)summonmodeLayout.Columns["Unk29"].Offset, 804);  //Do Thundaga

        //Shiva
        row = summonmodeTable.GetRow(5)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 992);   //Do Freeze   

        //Do Odin cancelable attack
        row = actionTable!.GetRow((uint)comboTable.GetRow(2, 0)!.GetInt32((uint)comboLayout.Columns["Unk2"].Offset))!;  //Get first attack in Odin's combo

        row.SetInt32((uint)actionLayout.Columns["MovementType2"].Offset, 5);

        //Do Heavensward -> Land
        row = playercommandbuilderTable!.GetRow(23)!;

        if (row.GetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset) == 302) row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 3133);
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

        int[] groundBaseVanilla = new int[] { _extras.attack1Base, _extras.attack2Base, _extras.attack3Base, _extras.attack4Base };
        int[] airBaseVanilla = new int[] { _extras.airattack1Base, _extras.airattack2Base, _extras.airattack3Base };

        int[] groundLBVanilla = new int[] { _extras.attackLB1, _extras.attackLB2, _extras.attackLB3, _extras.attackLB4 };
        int[] airLBVanilla = new int[] { _extras.airattackLB1, _extras.airattackLB2, _extras.airattackLB3 };

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

            if (i == 0)
            {
                row.SetInt32((uint)comboLayout.Columns["Unk20"].Offset, _extras.unk20AttackBase);
                row.SetInt32((uint)comboLayout.Columns["Unk12"].Offset, _extras.sheatedAttackBase);
                row.SetInt32((uint)comboLayout.Columns["Unk14"].Offset, _extras.sheatedWalkAttackBase);
                row.SetInt32((uint)comboLayout.Columns["Unk15"].Offset, _extras.sheatedRunAttackBase);
                row.SetInt32((uint)comboLayout.Columns["Unk16"].Offset, _extras.unsheathedWalkAttackBase);
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

            row = comboTable!.GetRow(24, i)!;   //Key1 24 is Berserker combo

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

        row = comboTable!.GetRow(30, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 0);

        row = comboTable!.GetRow(24, 0)!;

        row.SetInt32((uint)comboLayout.Columns["Unk27"].Offset, 0);

        //SUMMONMODE
        //Ultima
        row = summonmodeTable!.GetRow(10)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 1561);  //Undo Aether Ray
        row.SetInt32((uint)summonmodeLayout.Columns["Unk29"].Offset, 1573); //Undo Aether Lance

        //Ramuh
        row = summonmodeTable.GetRow(4)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 210);   //Undo Blind Exact
        row.SetInt32((uint)summonmodeLayout.Columns["Unk29"].Offset, 1567);  //Undo Thundaga

        //Shiva
        row = summonmodeTable.GetRow(5)!;
        row.SetInt32((uint)summonmodeLayout.Columns["Unk5"].Offset, 213);   //Undo Freeze  

        //Undo Odin cancelable attack
        row = actionTable!.GetRow((uint)comboTable.GetRow(2, 0)!.GetInt32((uint)comboLayout.Columns["Unk2"].Offset))!;  //Get first attack in Odin's combo

        row.SetInt32((uint)actionLayout.Columns["MovementType2"].Offset, 2);

        //Undo Heavensward -> Land
        row = playercommandbuilderTable!.GetRow(23)!;

        if (row.GetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset) == 3133) row.SetInt32((uint)playercommandbuilderLayout.Columns["Unk20"].Offset, 302);
    }

    //public const ushort TYPE_SKILL = 632;

    //private void DoChangeSkillTree()
    //{
    //    Span<NexUnionElement> skillArray = 
    //    [
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 187 },    //Shift Wave
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 186 },    //Logos Unleashed
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 188 },    //Deadly Gouge
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 189 },    //Deadly Wheel
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 190 },    //Deadly Gambit
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 191 },    //Blind Drive
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 192 },    //Blind Storm
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 193 },    //Blizzaga
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 194 },    //Dark Parry
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 195 },    //Dark Juggle
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 196 },    //Flash of Steel
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 197 },    //Mirage
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 198 },    //Steel Finish
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 199 },    //Serpent's Blast
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 200 },    //Serpent's Launcher
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 201 },    //Serpent's Wisdom
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 202 },    //Hindsight Slash
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 203 },    //Air Lunge
    //    ];

    //    Span<NexUnionElement> unionArray;
    //    string column = "Unk2";

    //    INexRow? row = uiarrayTable!.GetRow(1004, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[3] = skillArray[0];

    //    row = uiarrayTable!.GetRow(1005, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[3] = skillArray[0];

    //    row = uiarrayTable!.GetRow(1006, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[2];
    //    unionArray[2] = skillArray[3];
    //    unionArray[3] = skillArray[4];

    //    row = uiarrayTable!.GetRow(1007, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[5];
    //    unionArray[2] = skillArray[6];

    //    row = uiarrayTable!.GetRow(1010, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[7];

    //    row = uiarrayTable!.GetRow(1011, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[2] = skillArray[10];
    //    unionArray[3] = skillArray[11];
    //    unionArray[4] = skillArray[12];

    //    row = uiarrayTable!.GetRow(1012, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[13];
    //    unionArray[2] = skillArray[14];
    //    unionArray[3] = skillArray[15];

    //    row = uiarrayTable!.GetRow(1003, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[7] = skillArray[16];
    //    unionArray[6] = skillArray[8];
    //    unionArray[8] = skillArray[9];

    //    row = uiarrayTable!.GetRow(1003, 1)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[6] = skillArray[1];
    //    unionArray[3] = skillArray[17];

    //    //Console.WriteLine("Skills Changed");
    //}

    //private void UndoChangeSkillTree()
    //{
    //    Span<NexUnionElement> skillArray = 
    //    [
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 19 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 28 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 22 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 23 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 24 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 30 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 31 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 45 },  
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 9 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 3 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 51 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 52 },
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 53 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 55 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 56 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 57 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 12 },    
    //        new NexUnionElement() { Type = TYPE_SKILL, Value = 5 },   
    //    ];

    //    Span<NexUnionElement> unionArray;
    //    string column = "Unk2";

    //    INexRow? row = uiarrayTable!.GetRow(1004, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[3] = skillArray[0];

    //    row = uiarrayTable!.GetRow(1005, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[3] = skillArray[0];

    //    row = uiarrayTable!.GetRow(1006, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[2];
    //    unionArray[2] = skillArray[3];
    //    unionArray[3] = skillArray[4];

    //    row = uiarrayTable!.GetRow(1007, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[5];
    //    unionArray[2] = skillArray[6];

    //    row = uiarrayTable!.GetRow(1010, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[7];

    //    row = uiarrayTable!.GetRow(1011, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[2] = skillArray[10];
    //    unionArray[3] = skillArray[11];
    //    unionArray[4] = skillArray[12];

    //    row = uiarrayTable!.GetRow(1012, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[1] = skillArray[13];
    //    unionArray[2] = skillArray[14];
    //    unionArray[3] = skillArray[15];

    //    row = uiarrayTable!.GetRow(1003, 0)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[7] = skillArray[16];
    //    unionArray[6] = skillArray[8];
    //    unionArray[8] = skillArray[9];

    //    row = uiarrayTable!.GetRow(1003, 1)!;
    //    unionArray = row.GetUnionArrayView((uint)uiarrayLayout.Columns[column].Offset);
    //    unionArray[6] = skillArray[1];
    //    unionArray[3] = skillArray[17];
    //}

    private bool[] GetDualSenseKeys(DualSenseAPI.State.DualSenseInputState state)
    {
        bool currentSpecialButton = false;
        bool currentFinisherButton = false;
        bool currentDirectionalHoldButton = false;
        // bool currentFlyDown;
        // bool currentFlyUp;

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

        switch (_configuration.directionalHoldKeybind)
        {
            case ButtonFlags.DPadUp:
                currentDirectionalHoldButton = state.DPadUpButton;
                break;
            case ButtonFlags.DPadDown:
                currentDirectionalHoldButton = state.DPadDownButton;
                break;
            case ButtonFlags.DPadLeft:
                currentDirectionalHoldButton = state.DPadLeftButton;
                break;
            case ButtonFlags.DPadRight:
                currentDirectionalHoldButton = state.DPadRightButton;
                break;
            case ButtonFlags.LeftBumper:
                currentDirectionalHoldButton = state.L1Button;
                break;
            case ButtonFlags.RightBumper:
                currentDirectionalHoldButton = state.R1Button;
                break;
            case ButtonFlags.LeftThumb:
                currentDirectionalHoldButton = state.L3Button;
                break;
            case ButtonFlags.RightThumb:
                currentDirectionalHoldButton = state.R3Button;
                break;
            case ButtonFlags.Start:
                currentDirectionalHoldButton = state.MenuButton;
                break;
            case ButtonFlags.Select:
                currentDirectionalHoldButton = state.TouchpadButton;
                break;
            case ButtonFlags.X_Square:
                currentDirectionalHoldButton = state.SquareButton;
                break;
            case ButtonFlags.Y_Triangle:
                currentDirectionalHoldButton = state.TriangleButton;
                break;
            case ButtonFlags.A_Cross:
                currentDirectionalHoldButton = state.CrossButton;
                break;
            case ButtonFlags.B_Circle:
                currentDirectionalHoldButton = state.CircleButton;
                break;
        }

        return [currentSpecialButton, currentFinisherButton, currentDirectionalHoldButton];
    }

    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Input Configuration Updated: Applying");

        if (_configuration.isControllerEnabled && !controllerInputThread!.IsAlive)
        {
            controllerInputThread = new Thread(ControllerInputThread);
            controllerInputThread.IsBackground = true;
            controllerInputThread.Start();
        }

        if (_configuration.isDualSenseEnabled && !dualSenseInputThread!.IsAlive)
        {
            dualSenseInputThread = new Thread(DualSenseInputThread);
            dualSenseInputThread.IsBackground = true;
            dualSenseInputThread.Start();
        }
    }

    public override void ConfigurationUpdated(Extras extras)
    {
        // Apply settings from configuration.
        // ... your code here.
        _extras = extras;
        _logger.WriteLine($"[{_modConfig.ModId}] Combo Configuration Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}