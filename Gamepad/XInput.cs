using System.Runtime.InteropServices;

namespace CanEmAll.SplitScreenMod.Gamepad;

internal static class XInput
{
    private const uint ErrorSuccess = 0;

    // Flattened XINPUT_STATE and XINPUT_GAMEPAD layout.
    [StructLayout(LayoutKind.Sequential)]
    public struct State
    {
        public uint PacketNumber;
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLeftX;
        public short ThumbLeftY;
        public short ThumbRightX;
        public short ThumbRightY;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint GetStateNative(uint userIndex, out State state);

    public static bool TryGetState(int userIndex, out State state) =>
        GetStateNative((uint)userIndex, out state) == ErrorSuccess;
}
