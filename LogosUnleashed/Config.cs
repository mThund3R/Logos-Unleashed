using System.ComponentModel;
using ff16.gameplay.logosunleashed.Template.Configuration;
using GlobalKeyInterceptor;
using Reloaded.Mod.Interfaces.Structs;

using SharpDX.XInput;

namespace ff16.gameplay.logosunleashed.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.
    
        By default, configuration saves as "Config.json" in mod user config folder.    
        Need more config files/classes? See Configuration.cs
    
        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    public enum ButtonTriggerFlags
    {
        None = 0,
        DPadUp = 1,
        DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,
        Start = 0x10,
        Select = 0x20,
        LeftThumb = 0x40,
        RightThumb = 0x80,
        LeftBumper = 0x100,
        RightBumper = 0x200,
        A_Cross = 0x1000,
        B_Circle = 0x2000,
        X_Square = 0x4000,
        Y_Triangle = 0x8000,
        LeftTrigger = 69,   //Useless value
        RightTrigger = 420  //Useless value
    }

    public enum ButtonFlags
    {
        None = 0,
        DPadUp = 1,
        DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,
        Start = 0x10,
        Select = 0x20,
        LeftThumb = 0x40,
        RightThumb = 0x80,
        LeftBumper = 0x100,
        RightBumper = 0x200,
        A_Cross = 0x1000,
        B_Circle = 0x2000,
        X_Square = 0x4000,
        Y_Triangle = 0x8000,
    }

    [DisplayName("Is XInput Controller Enabled?")]
    [Description("Whether custom XInput gamepad input is enabled.")]
    [DefaultValue(true)]
    [Category(".ControllerInput")]
    public bool isControllerEnabled { get; set; } = true;

    [DisplayName("Is DualSense Controller Enabled?")]
    [Description("Whether custom DualSense controller input is enabled.\nEXPERIMENTAL!")]
    [DefaultValue(false)]
    [Category(".ControllerInput")]
    public bool isDualSenseEnabled { get; set; } = false;

    [DisplayName("Is Keyboard Enabled?")]
    [Description("Whether custom keyboard input is enabled. \nWARNING: This is a global key interceptor, which will capture keys even if the game is not focused!\nFurthermore, disabling this at runtime will only apply changes on game restart.")]
    [DefaultValue(true)]
    [Category(".KeyboardInput")]
    public bool isKeyboardEnabled { get; set; } = true;

    [DisplayName("Are Directional Inputs Limited To Button Hold?")]
    [Description("If enabled, diretional inputs can only be performed while holding a configurable button.")]
    [DefaultValue(false)]
    [Category(".GeneralInput")]
    public bool isDirectionalButtonHold { get; set; } = false;

    [DisplayName("[Gamepad / DualSense] Enable Directional Inputs Keybind")]
    [Description("When \"Are Directional Inputs Limited To Button Hold\" is enabled, hold this keybind to enable directional inputs.\nRecommended to use the in-game lock on button, and to set lock on mode on hold.")]
    [DefaultValue(ButtonFlags.LeftBumper)]
    [Category(".ControllerInput")]
    public ButtonFlags directionalHoldKeybind { get; set; } = ButtonFlags.LeftBumper;

    [DisplayName("[Keyboard] Enable Directional Inputs Keybind")]
    [Description("When \"Are Directional Inputs Limited To Button Hold\" is enabled, hold this keybind to enable directional inputs.\nRecommended to use the in-game lock on button, and to set lock on mode on hold.")]
    [DefaultValue(Key.LeftShift)]
    [Category(".KeyboardInput")]
    public Key directionalHoldKeybindKeyboard { get; set; } = Key.LeftShift;

    [DisplayName("[Gamepad / DualSense] Combo Finisher Keybind")]
    [Description("Hold this keybind and attack to perform combo finishers.")]
    [DefaultValue(ButtonFlags.LeftThumb)]
    [Category(".ControllerInput")]
    public ButtonFlags alternateComboKeybind { get; set; } = ButtonFlags.LeftThumb;

    [DisplayName("[Keyboard] Combo Finisher Keybind")]
    [Description("Hold this keybind and attack to perform combo finishers.")]
    [DefaultValue(Key.X)]
    [Category(".KeyboardInput")]
    public Key alternateComboKeybindKeyboard { get; set; } = Key.X;

    [DisplayName("[Keyboard] Change Skill Tree Page Keybind [UNUSED]")]
    [Description("[OUTDATED] Press this keybind, then refresh Skill Tree page to view Logos Unleashed custom skills.\nOn controller, press LB / L1 and RT / R1 together.")]
    [DefaultValue(Key.RightShift)]
    [Category(".KeyboardInput")]
    public Key changeSkillTreeKeybindKeyboard { get; set; } = Key.RightShift;


    [DisplayName("[Gamepad / DualSense] Special Attack Keybind")]
    [Description("Special (non-directional) attack keybind.")]
    [DefaultValue(ButtonTriggerFlags.RightTrigger)]
    [Category(".ControllerInput")]
    public ButtonTriggerFlags specialAttackKeybind { get; set; } = ButtonTriggerFlags.RightTrigger;

    [DisplayName("[Keyboard] Special Attack Keybind")]
    [Description("Special (non-directional) attack keybind.")]
    [DefaultValue(Key.CapsLock)]
    [Category(".KeyboardInput")]
    public Key specialAttackKeybindKeyboard { get; set; } = Key.CapsLock;
    
    [DisplayName("[Gamepad] LStick Deadzone")]
    [Description("This controls the deadzone of the left stick inputs.")]
    [DefaultValue(16500)]
    [Category(".ControllerInput")]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 32767.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 100,
        isSnapToTickEnabled: true,
        tickPlacement:SliderControlTickPlacement.None,
        showTextField: true,
        isTextFieldEditable: true)]
    public int deadZoneVertical { get; set; } = 16500;

    [DisplayName("[DualSense] Polling Rate")]
    [Description("This controls the polling rate in ms of all DualSense inputs.\nYou can lower it to 1 if you're using a DualSense Edge. Do not set to 0.")]
    [DefaultValue((uint)4)]
    [Category(".ControllerInput")]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 1000.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 1,
        isSnapToTickEnabled: true,
        tickPlacement:SliderControlTickPlacement.None,
        showTextField: true,
        isTextFieldEditable: true)]
    public uint dualSensePollingRate { get; set; } = 4;

    [DisplayName("[DualSense] LStick Deadzone")]
    [Description("This controls the deadzone of the left stick inputs.")]
    [DefaultValue(0.3f)]
    [Category(".ControllerInput")]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 1.0,
        smallChange: 0.01f,
        largeChange: 0.1f,
        tickFrequency: 1,
        isSnapToTickEnabled: false,
        tickPlacement:SliderControlTickPlacement.None,
        showTextField: true,
        isTextFieldEditable: true)]
    public float deadZoneVerticalDualSense { get; set; } = 0.3f;

    [DisplayName("[Gamepad] Trigger Deadzone")]
    [Description("This controls the deadzone of the trigger (R2 / RT, L2 / LT) inputs.")]
    [DefaultValue(20)]
    [Category(".ControllerInput")]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 255.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 10,
        isSnapToTickEnabled: false,
        tickPlacement:SliderControlTickPlacement.None,
        showTextField: true,
        isTextFieldEditable: true)]
    public int deadZoneR2 { get; set; } = 20;

    [DisplayName("[DualSense] Trigger Deadzone")]
    [Description("This controls the deadzone of the trigger (R2, L2) inputs.")]
    [DefaultValue(0.08f)]
    [Category(".ControllerInput")]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 1.0,
        smallChange: 0.01f,
        largeChange: 0.1f,
        tickFrequency: 1,
        isSnapToTickEnabled: false,
        tickPlacement:SliderControlTickPlacement.None,
        showTextField: true,
        isTextFieldEditable: true)]
    public float deadZoneR2DualSense { get; set; } = 0.08f;

    // [DisplayName("[Gamepad / DualSense] Upward Flight Keybind")]
    // [Description("Press alongside Special Attack keybind to fly upwards with Wings of Flight.")]
    // [DefaultValue(ButtonFlags.A_Cross)]
    // [Category(".ControllerInput")]
    // public ButtonFlags flyUpKeybindController { get; set; } = ButtonFlags.A_Cross;

    // [DisplayName("[Keyboard] Upward Flight Keybind")]
    // [Description("Press alongside Special Attack keybind to fly upwards with Wings of Flight.")]
    // [DefaultValue(Key.Space)]
    // [Category(".KeyboardInput")]
    // public Key flyUpKeybindKeyboard { get; set; } = Key.Space;

    // [DisplayName("[Gamepad / DualSense] Downward Flight Keybind")]
    // [Description("Press alongside Special Attack keybind to fly downwards with Wings of Flight.")]
    // [DefaultValue(ButtonFlags.LeftThumb)]
    // [Category(".ControllerInput")]
    // public ButtonFlags flyDownKeybindController { get; set; } = ButtonFlags.LeftThumb;

    // [DisplayName("[Keyboard] Downward Flight Keybind")]
    // [Description("Press alongside Special Attack keybind to fly downwards with Wings of Flight.")]
    // [DefaultValue(Key.Ctrl)]
    // [Category(".KeyboardInput")]
    // public Key flyDownKeybindKeyboard { get; set; } = Key.Ctrl;
}

public class Extras : Configurable<Extras>
{
    [DisplayName("Attack I")]
    [DefaultValue(503)]
    [Category("Base / LB Combo String Finisher")]
    public int attack1 { get; set; } = 503; //Titanic Counter finisher

    [DisplayName("Attack II")]
    [DefaultValue(5103)]
    [Category("Base / LB Combo String Finisher")]
    public int attack2 { get; set; } = 5103; //Odin lunge finisher

    [DisplayName("Attack III")]
    [DefaultValue(5106)]
    [Category("Base / LB Combo String Finisher")]
    public int attack3 { get; set; } = 5106; //Garuda Gouge

    [DisplayName("Attack IV")]
    [DefaultValue(5107)]
    [Category("Base / LB Combo String Finisher")]
    public int attack4 { get; set; } = 5107; //Slash and punch ground finisher

    // [DisplayName("Magic Burst I")]
    // [DefaultValue(188)]
    // [Category("Base Combo String")]
    // public int magicburst1 { get; set; } = 188;

    // [DisplayName("Magic Burst II")]
    // [DefaultValue(881)]
    // [Category("Base Combo String")]
    // public int magicburst2 { get; set; } = 881;

    // [DisplayName("Magic Burst III")]
    // [DefaultValue(635)]
    // [Category("Base Combo String")]
    // public int magicburst3 { get; set; } = 635;

    // [DisplayName("Magic Burst IV")]
    // [DefaultValue(506)]
    // [Category("Base Combo String")]
    // public int magicburst4 { get; set; } = 506;

    [DisplayName("Air Attack I")]
    [DefaultValue(506)]
    [Category("Base / LB Combo String Finisher")]
    public int airattack1 { get; set; } = 506;    //Titanic Counter finisher air

    [DisplayName("Air Attack II")]
    [DefaultValue(715)]
    [Category("Base / LB Combo String Finisher")]
    public int airattack2 { get; set; } = 715;    //Shiva small freeze air

    [DisplayName("Air Attack III")]
    [DefaultValue(361)]
    [Category("Base / LB Combo String Finisher")]
    public int airattack3 { get; set; } = 361;    //Shift Shot (can be changed through directional input)

    // [DisplayName("Air Magic Burst I")]
    // [DefaultValue(0)]
    // [Category("Base Combo String")]
    // public int airmagicburst1 { get; set; } = 0;

    // [DisplayName("Air Magic Burst II")]
    // [DefaultValue(0)]
    // [Category("Base Combo String")]
    // public int airmagicburst2 { get; set; } = 0;

    // [DisplayName("Air Magic Burst III")]
    // [DefaultValue(0)]
    // [Category("Base Combo String")]
    // public int airmagicburst3 { get; set; } = 0;


    [DisplayName("Attack I")]
    [DefaultValue(503)]
    [Category("Logos Combo String Finisher")]
    public int attackLogosFinisher1 { get; set; } = 503; //Titanic Counter finisher

    [DisplayName("Attack II")]
    [DefaultValue(5112)]
    [Category("Logos Combo String Finisher")]
    public int attackLogosFinisher2 { get; set; } = 5112; //Heaven's Cloud x4 into Gungnir cancel

    [DisplayName("Attack III")]
    [DefaultValue(5110)]
    [Category("Logos Combo String Finisher")]
    public int attackLogosFinisher3 { get; set; } = 5110; //Dancing Steel into Upheaval

    [DisplayName("Attack IV")]
    [DefaultValue(5107)]
    [Category("Logos Combo String Finisher")]
    public int attackLogosFinisher4 { get; set; } = 5107; //Slash and punch ground finisher

    [DisplayName("Air Attack I")]
    [DefaultValue(506)]
    [Category("Logos Combo String Finisher")]
    public int airattackLogosFinisher1 { get; set; } = 506;    //Titanic Counter finisher air

    [DisplayName("Air Attack II")]
    [DefaultValue(715)]
    [Category("Logos Combo String Finisher")]
    public int airattackLogosFinisher2 { get; set; } = 715;    //Shiva small freeze air

    [DisplayName("Air Attack III")]
    [DefaultValue(361)]
    [Category("Logos Combo String Finisher")]
    public int airattackLogosFinisher3 { get; set; } = 361;    //Shift Shot (can be changed through directional input)


    [DisplayName("Attack I")]
    [DefaultValue(293)]
    [Category("LB Combo String")]
    public int attackLB1 { get; set; } = 293;

    [DisplayName("Attack II")]
    [DefaultValue(294)]
    [Category("LB Combo String")]
    public int attackLB2 { get; set; } = 294;

    [DisplayName("Attack III")]
    [DefaultValue(295)]
    [Category("LB Combo String")]
    public int attackLB3 { get; set; } = 295;

    [DisplayName("Attack IV")]
    [DefaultValue(296)]
    [Category("LB Combo String")]
    public int attackLB4 { get; set; } = 296;

    // [DisplayName("Magic Burst I")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int magicburstLB1 { get; set; } = 0;

    // [DisplayName("Magic Burst II")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int magicburstLB2 { get; set; } = 0;

    // [DisplayName("Magic Burst III")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int magicburstLB3 { get; set; } = 0;

    // [DisplayName("Magic Burst IV")]
    // [DefaultValue(506)]
    // [Category("LB Combo String")]
    // public int magicburstLB4 { get; set; } = 506;

    [DisplayName("Air Attack I")]
    [DefaultValue(297)]
    [Category("LB Combo String")]
    public int airattackLB1 { get; set; } = 297;

    [DisplayName("Air Attack II")]
    [DefaultValue(298)]
    [Category("LB Combo String")]
    public int airattackLB2 { get; set; } = 298;

    [DisplayName("Air Attack III")]
    [DefaultValue(299)]
    [Category("LB Combo String")]
    public int airattackLB3 { get; set; } = 299;

    // [DisplayName("Air Magic Burst I")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int airmagicburstLB1 { get; set; } = 0;

    // [DisplayName("Air Magic Burst II")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int airmagicburstLB2 { get; set; } = 0;

    // [DisplayName("Air Magic Burst III")]
    // [DefaultValue(0)]
    // [Category("LB Combo String")]
    // public int airmagicburstLB3 { get; set; } = 0;

    [DisplayName("Attack I")]
    [DefaultValue(139)]
    [Category("Base Combo String")]
    public int attack1Base { get; set; } = 139;

    [DisplayName("Attack II")]
    [DefaultValue(154)]
    [Category("Base Combo String")]
    public int attack2Base { get; set; } = 154;

    [DisplayName("Attack III")]
    [DefaultValue(155)]
    [Category("Base Combo String")]
    public int attack3Base { get; set; } = 155;

    [DisplayName("Attack IV")]
    [DefaultValue(156)]
    [Category("Base Combo String")]
    public int attack4Base { get; set; } = 156;

    [DisplayName("Air Attack I")]
    [DefaultValue(157)]
    [Category("Base Combo String")]
    public int airattack1Base { get; set; } = 157;

    [DisplayName("Air Attack II")]
    [DefaultValue(158)]
    [Category("Base Combo String")]
    public int airattack2Base { get; set; } = 158;

    [DisplayName("Air Attack III")]
    [DefaultValue(159)]
    [Category("Base Combo String")]
    public int airattack3Base { get; set; } = 159;

    [DisplayName("Sheathed Attack I")]
    [DefaultValue(140)]
    [Category("Base Combo String")]
    public int sheatedAttackBase { get; set; } = 140;

    [DisplayName("Sheathed Walk Attack I")]
    [DefaultValue(142)]
    [Category("Base Combo String")]
    public int sheatedWalkAttackBase { get; set; } = 142;

    [DisplayName("Sheathed Run Attack I")]
    [DefaultValue(143)]
    [Category("Base Combo String")]
    public int sheatedRunAttackBase { get; set; } = 143;

    [DisplayName("Unsheathed Walk Attack I")]
    [DefaultValue(141)]
    [Category("Base Combo String")]
    public int unsheathedWalkAttackBase { get; set; } = 141;

    [DisplayName("Unk20 Attack I")]
    [DefaultValue(139)]
    [Category("Base Combo String")]
    [Description("Still uncertain on what this does, but it's set to same ActionId as normal Attack I, and so should you")]
    public int unk20AttackBase { get; set; } = 139;


    // [DisplayName("Double Slider")]
    // [Description("This is a double that uses a slider control without any frills.")]
    // [DefaultValue(0.5)]
    // [SliderControlParams(minimum: 0.0, maximum: 1.0)]
    // public double DoubleSlider { get; set; } = 0.5;

    // [DisplayName("File Picker")]
    // [Description("This is a sample file picker.")]
    // [DefaultValue("")]
    // [FilePickerParams(title:"Choose a File to load from")]
    // public string File { get; set; } = "";

    // [DisplayName("Folder Picker")]
    // [Description("Opens a file picker but locked to only allow folder selections.")]
    // [DefaultValue("")]
    // [FolderPickerParams(
    //     initialFolderPath: Environment.SpecialFolder.Desktop,
    //     userCanEditPathText: false,
    //     title: "Custom Folder Select",
    //     okButtonLabel: "Choose Folder",
    //     fileNameLabel: "ModFolder",
    //     multiSelect: true,
    //     forceFileSystem: true)]
    // public string Folder { get; set; } = "";
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
