namespace SteamControllerBridge.Bridge;

internal sealed class MouseEmulator
{
    private const int SensitivityDivisor = 65;
    private bool _touching;
    private bool _leftDown;
    private short _lastX;
    private short _lastY;

    public void Update(SteamControllerInput input, BridgeOptions options)
    {
        if (!options.TrackpadMouseEnabled)
        {
            Reset();
            return;
        }

        var hasPad = TryGetPad(input, options.TrackpadMouseSource, out var touching, out var clicked, out var x, out var y);
        if (!hasPad || !touching)
        {
            _touching = false;
            SetLeftButton(false);
            return;
        }

        if (_touching)
        {
            var dx = (x - _lastX) / SensitivityDivisor;
            var dy = -((y - _lastY) / SensitivityDivisor);
            if (dx != 0 || dy != 0)
            {
                MouseInput.Move(dx, dy);
            }
        }

        _lastX = x;
        _lastY = y;
        _touching = true;
        SetLeftButton(options.TrackpadClickEnabled && clicked);
    }

    public void Reset()
    {
        _touching = false;
        SetLeftButton(false);
    }

    private static bool TryGetPad(
        SteamControllerInput input,
        TrackpadMouseSource source,
        out bool touching,
        out bool clicked,
        out short x,
        out short y)
    {
        if ((source == TrackpadMouseSource.Left || source == TrackpadMouseSource.Both) && input.LeftPadTouched)
        {
            touching = true;
            clicked = input.LeftPadClicked;
            x = input.LeftPadX;
            y = input.LeftPadY;
            return true;
        }

        if ((source == TrackpadMouseSource.Right || source == TrackpadMouseSource.Both) && input.RightPadTouched)
        {
            touching = true;
            clicked = input.RightPadClicked;
            x = input.RightPadX;
            y = input.RightPadY;
            return true;
        }

        touching = false;
        clicked = false;
        x = 0;
        y = 0;
        return false;
    }

    private void SetLeftButton(bool down)
    {
        if (_leftDown == down)
        {
            return;
        }

        MouseInput.SetLeftButton(down);
        _leftDown = down;
    }
}
