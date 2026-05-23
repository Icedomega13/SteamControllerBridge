namespace SteamControllerBridge.Bridge;

internal sealed class GyroMouseEmulator
{
    private const int DeadZone = 45;
    private const double Sensitivity = 0.018;
    private bool _active;
    private double _biasX;
    private double _biasY;
    private double _biasZ;

    public void Update(SteamControllerInput input, BridgeOptions options)
    {
        if (!options.GyroMouseEnabled || !input.HasGyro)
        {
            Reset();
            return;
        }

        var shouldActivate = IsActive(input, options.GyroMouseActivation);
        if (!shouldActivate)
        {
            LearnBias(input, fast: false);
            _active = false;
            return;
        }

        if (!_active)
        {
            LearnBias(input, fast: true);
            _active = true;
            return;
        }

        var yaw = input.GyroZ - _biasZ;
        var pitch = input.GyroX - _biasX;

        var dx = ApplyDeadZone(yaw);
        var dy = ApplyDeadZone(-pitch);
        if (dx != 0 || dy != 0)
        {
            MouseInput.Move(dx, dy);
        }
    }

    public void Reset()
    {
        _active = false;
    }

    private void LearnBias(SteamControllerInput input, bool fast)
    {
        var weight = fast ? 0.65 : 0.04;
        _biasX = Lerp(_biasX, input.GyroX, weight);
        _biasY = Lerp(_biasY, input.GyroY, weight);
        _biasZ = Lerp(_biasZ, input.GyroZ, weight);
    }

    private static int ApplyDeadZone(double value)
    {
        if (Math.Abs(value) < DeadZone)
        {
            return 0;
        }

        return (int)Math.Clamp(value * Sensitivity, -24, 24);
    }

    private static bool IsActive(SteamControllerInput input, GyroMouseActivation activation)
    {
        return activation switch
        {
            GyroMouseActivation.LeftTrigger => input.LeftTriggerActive,
            GyroMouseActivation.RightTrigger => input.RightTriggerActive,
            GyroMouseActivation.LeftPadTouch => input.LeftPadTouched,
            GyroMouseActivation.RightPadTouch => input.RightPadTouched,
            GyroMouseActivation.Always => true,
            _ => false
        };
    }

    private static double Lerp(double current, double target, double weight)
    {
        return current + ((target - current) * weight);
    }
}
