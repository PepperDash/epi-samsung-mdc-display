using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace PepperDashPluginSamsungMdcDisplay.Tests.Mock;

/// <summary>
/// Runs the mock panel against the driver's own formatting and parsing (SamsungMdcDriverCopy).
/// Tests named KnownBug_* pin current driver behaviour that disagrees with the MDC spec; when the
/// driver is fixed they fail on purpose, and should be flipped to assert the fixed behaviour.
/// </summary>
public class MockSamsungMdcResponderTests
{
    private const byte SiteId = 0x00; // "id": "00" in the site config
    private readonly ITestOutputHelper output;

    public MockSamsungMdcResponderTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static MockSamsungMdcResponder Panel(bool powerOn = true, Action<MockSamsungMdcOptions>? configure = null)
    {
        var options = new MockSamsungMdcOptions { PowerOn = powerOn };
        configure?.Invoke(options);
        return new MockSamsungMdcResponder(SiteId, options);
    }

    private static SamsungMdcDriverCopy SiteDriver() => new(SiteId, volumeLowerLimit: 0, volumeUpperLimit: 99);

    // One receive event per reply frame, like MockSamsungMdcCommunication
    private static IList<byte[]> Exchange(SamsungMdcDriverCopy driver, MockSamsungMdcResponder panel, byte[] command)
    {
        var replies = panel.Respond(command);
        foreach (var reply in replies) driver.BytesReceived(reply);
        return replies;
    }

    private static string Hex(byte[] bytes) => string.Join(" ", bytes.Select(b => b.ToString("X2")));

    // ---- Framing, checksum, ID ----

    [Fact]
    public void Driver_Frames_Have_Valid_Checksums_And_Lengths()
    {
        var driver = SiteDriver();
        var frames = new[]
        {
            driver.StatusGet(), driver.PowerOn(), driver.PowerOff(), driver.PowerGet(), driver.InputGet(),
            driver.VolumeGet(), driver.MuteOn(), driver.MuteOff(), driver.MuteGet(), driver.LedProductMonitorGet(),
            driver.InputGeneric(0x21), driver.SendVolumeCommand(32768),
        };

        foreach (var frame in frames)
        {
            MockSamsungMdcResponder.FrameLength(frame).Should().Be(frame.Length, Hex(frame));
            frame[frame.Length - 1].Should().Be(MockSamsungMdcResponder.Checksum(frame, frame.Length - 1), Hex(frame));
        }
    }

    [Fact]
    public void Spec_Examples_Match_Checksum()
    {
        // Spec 1.2.1: power on to ID 0xFE checksums to 0x11 (0x11 + 0xFE + 0x01 + 0x01 = 0x111)
        MockSamsungMdcResponder.Command(0x11, 0xFE, 0x01).Should().Equal(0xAA, 0x11, 0xFE, 0x01, 0x01, 0x11);
        // Spec 2.1.11: power off to ID 1 is AA 11 01 01 00 13 (the spec's own example drops the length byte)
        MockSamsungMdcResponder.Command(0x11, 0x01, 0x00).Should().Equal(0xAA, 0x11, 0x01, 0x01, 0x00, 0x13);
    }

    [Fact]
    public void Status_Ack_Is_Spec_Layout()
    {
        var panel = Panel(configure: o => { o.Volume = 37; o.Muted = true; o.Input = "hdmi2"; });
        var reply = panel.Respond(SiteDriver().StatusGet()).Single();

        // AA FF id 09 'A' 00 power volume mute input aspect 00 00 cs
        reply.Take(13).Should().Equal(0xAA, 0xFF, 0x00, 0x09, 0x41, 0x00, 0x01, 37, 0x01, 0x23, 0x10, 0x00, 0x00);
        reply[13].Should().Be(MockSamsungMdcResponder.Checksum(reply, 13));
    }

    [Fact]
    public void Bad_Checksum_Is_Ignored()
    {
        var frame = SiteDriver().StatusGet();
        frame[frame.Length - 1] ^= 0xFF;
        Panel().Respond(frame).Should().BeEmpty();
    }

    [Fact]
    public void Other_Display_Id_Is_Ignored()
    {
        var other = new SamsungMdcDriverCopy(0x01, 0, 99);
        Panel().Respond(other.StatusGet()).Should().BeEmpty();
    }

    [Fact]
    public void Broadcast_Set_Applies_Without_Ack_And_Broadcast_Get_Answers()
    {
        var panel = Panel();
        var broadcast = new SamsungMdcDriverCopy(MockSamsungMdcResponder.BroadcastId, 0, 0);

        panel.Respond(broadcast.MuteOn()).Should().BeEmpty();
        var reply = panel.Respond(broadcast.MuteGet()).Single();
        reply.Take(7).Should().Equal(0xAA, 0xFF, SiteId, 0x03, 0x41, 0x13, 0x01);
        reply[7].Should().Be(0x57); // (FF + 00 + 03 + 41 + 13 + 01) & FF
    }

    [Fact]
    public void Unknown_Commands_Are_Silent()
    {
        // 0x15 picture size, 0x62 volume up/down: valid MDC commands this mock doesn't implement
        Panel().Respond(MockSamsungMdcResponder.Command(0x15, SiteId)).Should().BeEmpty();
        Panel().Respond(MockSamsungMdcResponder.Command(0x62, SiteId, 0x00)).Should().BeEmpty();
    }

    [Fact]
    public void FrameLength_Splits_Back_To_Back_And_Waits_For_Split_Frames()
    {
        var driver = SiteDriver();
        var buffer = new List<byte>();
        buffer.AddRange(driver.StatusGet());
        buffer.AddRange(driver.InputGeneric(0x23));

        MockSamsungMdcResponder.FrameLength(buffer).Should().Be(5);
        buffer.RemoveRange(0, 5);
        MockSamsungMdcResponder.FrameLength(buffer).Should().Be(6);

        MockSamsungMdcResponder.FrameLength(new List<byte> { 0xAA, 0x14, 0x00 }).Should().Be(0, "length byte not here yet");
        MockSamsungMdcResponder.FrameLength(new List<byte> { 0x00, 0xAA }).Should().Be(1, "noise byte is dropped");
    }

    // ---- Power ----

    [Fact]
    public void Off_Panel_Answers_Status_And_Power_And_Naks_The_Rest()
    {
        var driver = SiteDriver();
        var panel = Panel(powerOn: false);

        Exchange(driver, panel, driver.StatusGet());
        driver.PowerIsOn.Should().BeFalse();

        panel.Respond(driver.PowerGet()).Single()[6].Should().Be(0x00);
        panel.Respond(driver.InputGeneric(0x23)).Single().Should().Equal(0xAA, 0xFF, 0x00, 0x03, 0x4E, 0x14, 0x01, 0x65);
        panel.Respond(driver.MuteOn()).Single()[4].Should().Be(MockSamsungMdcResponder.Nak);
        panel.Respond(driver.SendVolumeCommand(1000)).Single()[4].Should().Be(MockSamsungMdcResponder.Nak);
    }

    [Fact]
    public void Off_Panel_Answers_Everything_When_NakWhileOff_Is_False()
    {
        var panel = Panel(powerOn: false, o => o.NakWhileOff = false);
        panel.Respond(SiteDriver().MuteOn()).Single()[4].Should().Be(MockSamsungMdcResponder.Ack);
    }

    [Fact]
    public void Power_On_Then_Off_Updates_Driver()
    {
        var driver = SiteDriver();
        var panel = Panel(powerOn: false);

        Exchange(driver, panel, driver.PowerOn());
        driver.PowerIsOn.Should().BeTrue();
        Exchange(driver, panel, driver.StatusGet());
        driver.PowerIsOn.Should().BeTrue();

        Exchange(driver, panel, driver.PowerOff());
        driver.PowerIsOn.Should().BeFalse();
        panel.Describe().Should().Contain("power=off");
    }

    // ---- Inputs ----

    [Fact]
    public void Every_Selectable_Driver_Input_Round_Trips()
    {
        var driver = SiteDriver();
        var panel = Panel();
        var mismatches = 0;

        // The "usb" port has no selector, so the driver can't select it
        var selectable = SamsungMdcDriverCopy.DefaultPorts.Where(p => p.Key != "usb").ToArray();
        foreach (var port in selectable)
        {
            var expected = Array.IndexOf(SamsungMdcDriverCopy.DefaultPorts, port) + 1;

            driver.CurrentInputNumber = 0;
            Exchange(driver, panel, driver.InputGeneric(port.Code));
            var afterSet = driver.CurrentInputNumber;

            driver.CurrentInputNumber = 0;
            Exchange(driver, panel, driver.StatusGet());
            var afterPoll = driver.CurrentInputNumber;

            if (afterSet != expected || afterPoll != expected)
            {
                mismatches++;
                output.WriteLine($"{port.Key} 0x{port.Code:X2}: set->{afterSet} poll->{afterPoll}, expected {expected}");
            }
        }

        output.WriteLine($"Inputs swept: {selectable.Length}, mismatches: {mismatches}");
        mismatches.Should().Be(0);
    }

    [Theory]
    [InlineData(0x22)] // HDMI1_PC: get only
    [InlineData(0x99)] // not an MDC input code (the driver's "usb" port)
    public void Set_Input_With_Get_Only_Or_Unknown_Code_Is_Nakd(byte code)
    {
        Panel().Respond(SiteDriver().InputGeneric(code)).Single()[4].Should().Be(MockSamsungMdcResponder.Nak);
    }

    [Fact]
    public void KnownBug_Hdmi_Pc_Variant_And_Internal_Usb_Are_Not_Recognised()
    {
        // A panel on HDMI1 with a PC signal reports 0x22 (spec 2.1.14: get-only code); Internal/USB is 0x62.
        // The driver only matches 0x21 / 0x99, so its input feedback goes stale.
        foreach (var reported in new[] { "hdmi1Pc", "internalUsb" })
        {
            var driver = SiteDriver();
            var panel = Panel();
            panel.SetInput(reported).Should().BeTrue();

            driver.CurrentInputNumber = 5;
            Exchange(driver, panel, driver.StatusGet());
            driver.CurrentInputNumber.Should().Be(5, $"{reported} matches no routing port");
        }
    }

    [Fact]
    public void KnownBug_SetInput_Rejects_The_Last_Input_Number()
    {
        // SetInput(int) bails on value >= InputPorts.Count, so the last port can never be picked by number
        var count = SamsungMdcDriverCopy.DefaultPorts.Length;
        SamsungMdcDriverCopy.SetInputAccepts(count - 1, count).Should().BeTrue();
        SamsungMdcDriverCopy.SetInputAccepts(count, count).Should().BeFalse();
    }

    // ---- Volume and mute ----

    [Theory]
    [InlineData(0, 99)]   // site config: volumeLowerLimit 0, volumeUpperLimit 99
    [InlineData(0, 0)]    // limits unset: unscaled 0-100
    [InlineData(20, 80)]
    public void Volume_Sweep_Every_Ui_Level(int lower, int upper)
    {
        var driver = new SamsungMdcDriverCopy(SiteId, lower, upper);
        var panel = Panel();
        var top = upper > lower ? upper : 100;
        var bottom = upper > lower ? lower : 0;

        int naks = 0, outOfRange = 0, feedbackMismatch = 0;
        var levelsReached = new HashSet<int>();

        for (var level = 0; level <= 65535; level++)
        {
            var frame = driver.SendVolumeCommand((ushort)level);
            var sent = frame[4];
            if (sent < bottom || sent > top) outOfRange++;

            var reply = Exchange(driver, panel, frame).Single();
            if (reply[4] != MockSamsungMdcResponder.Ack) { naks++; continue; }

            levelsReached.Add(reply[6]);
            var expectedFeedback = SamsungMdcDriverCopy.ToLevel(SamsungMdcDriverCopy.Scale(sent, bottom, top, 0, 65535));
            if (driver.VolumeLevelForSig != expectedFeedback) feedbackMismatch++;
        }

        output.WriteLine($"limits {lower}-{upper}: naks={naks} outOfRange={outOfRange} feedbackMismatch={feedbackMismatch} " +
                         $"distinct panel levels={levelsReached.Count} ({levelsReached.Min()}-{levelsReached.Max()})");

        naks.Should().Be(0);
        outOfRange.Should().Be(0);
        feedbackMismatch.Should().Be(0);
        levelsReached.Should().HaveCount(top - bottom + 1, "every panel level in range is reachable from the UI");
    }

    [Theory]
    [InlineData(0, 99)]
    [InlineData(0, 0)]
    [InlineData(20, 80)]
    public void Volume_Feedback_Echoed_Back_Does_Not_Drift(int lower, int upper)
    {
        // A UI that sends back the level it was given (e.g. a slider re-sent unchanged) must leave the panel alone
        var driver = new SamsungMdcDriverCopy(SiteId, lower, upper);
        var top = upper > lower ? upper : 100;
        var bottom = upper > lower ? lower : 0;
        var drifting = new List<int>();

        for (var v = bottom; v <= top; v++)
        {
            var panel = Panel(configure: o => o.Volume = v);
            Exchange(driver, panel, driver.VolumeGet());
            var reply = Exchange(driver, panel, driver.SendVolumeCommand(driver.VolumeLevelForSig)).Single();
            if (reply[6] != v) drifting.Add(v);
        }

        output.WriteLine($"limits {lower}-{upper}: {drifting.Count} of {top - bottom + 1} levels drift");
        drifting.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 99)]
    [InlineData(0, 0)]
    [InlineData(20, 80)]
    public void Single_Volume_Tap_Moves_Exactly_One_Step(int lower, int upper)
    {
        // ActionIncrementer.Go: first step = _lastVolumeSent (feedback level) +/- step, clamped to the ramp limits,
        // then sent through SendVolumeCommand
        var driver = new SamsungMdcDriverCopy(SiteId, lower, upper);
        var top = upper > lower ? upper : 100;
        var bottom = upper > lower ? lower : 0;
        var wrongUp = 0;
        var wrongDown = 0;

        for (var v = bottom; v <= top; v++)
        {
            foreach (var direction in new[] { 1, -1 })
            {
                var panel = Panel(configure: o => o.Volume = v);
                Exchange(driver, panel, driver.VolumeGet());
                var next = Math.Max(SamsungMdcDriverCopy.RampMin,
                    Math.Min(SamsungMdcDriverCopy.RampMax, driver.VolumeLevelForSig + direction * driver.RampStep));
                var reply = Exchange(driver, panel, driver.SendVolumeCommand((ushort)next)).Single();
                var expected = Math.Max(bottom, Math.Min(top, v + direction));
                if (reply[6] != expected)
                {
                    if (direction > 0) wrongUp++; else wrongDown++;
                }
            }
        }

        output.WriteLine($"limits {lower}-{upper}: step {driver.RampStep}, taps off by one: up {wrongUp}, down {wrongDown}");
        wrongUp.Should().Be(0);
        wrongDown.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 99)]
    [InlineData(0, 0)]
    [InlineData(20, 80)]
    public void Tap_Before_Slider_Feedback_Moves_Exactly_One_Step(int lower, int upper)
    {
        // A slider level, then a tap before the panel's feedback arrives: the tap starts from
        // _lastVolumeSent, which must be the level the device was sent, not the raw slider value
        // (0-100: slider 983 sends 1; 983 + 656 = 1639 would send 3)
        var top = upper > lower ? upper : 100;
        var bottom = upper > lower ? lower : 0;
        var wrong = new List<string>();

        for (var slider = 0; slider <= 65535; slider += 7)
        {
            foreach (var direction in new[] { 1, -1 })
            {
                var driver = new SamsungMdcDriverCopy(SiteId, lower, upper);
                int sent = driver.SendVolumeCommand((ushort)slider)[4];
                var next = Math.Max(SamsungMdcDriverCopy.RampMin,
                    Math.Min(SamsungMdcDriverCopy.RampMax, driver.LastVolumeSent + direction * driver.RampStep));
                int tapped = driver.SendVolumeCommand((ushort)next)[4];
                var expected = Math.Max(bottom, Math.Min(top, sent + direction));
                if (tapped != expected) wrong.Add($"{slider}{(direction > 0 ? "+" : "-")}: {sent} -> {tapped}");
            }
        }

        output.WriteLine($"limits {lower}-{upper}: {wrong.Count} taps off; first: {string.Join(", ", wrong.Take(3))}");
        wrong.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 99)]
    [InlineData(20, 80)]
    [InlineData(0, 0)]
    public void Volume_Ramp_Reaches_Exactly_The_Configured_Limits(int lower, int upper)
    {
        var driver = new SamsungMdcDriverCopy(SiteId, lower, upper);
        var top = upper > lower ? upper : 100;
        var bottom = upper > lower ? lower : 0;

        driver.SendVolumeCommand(SamsungMdcDriverCopy.RampMin)[4].Should().Be((byte)bottom);
        driver.SendVolumeCommand(SamsungMdcDriverCopy.RampMax)[4].Should().Be((byte)top);
    }

    [Fact]
    public void Panel_Level_Above_The_Upper_Limit_Clamps_Feedback()
    {
        // volumeUpperLimit 99 but the panel is at 100 (set at the panel): feedback is full scale, not a wrapped value
        var driver = SiteDriver();
        Exchange(driver, Panel(configure: o => o.Volume = 100), driver.VolumeGet());
        driver.VolumeLevelForSig.Should().Be(65535);
    }

    [Fact]
    public void Volume_Edges()
    {
        var panel = Panel();
        panel.Respond(MockSamsungMdcResponder.Command(0x12, SiteId, 0)).Single()[6].Should().Be(0);
        panel.Respond(MockSamsungMdcResponder.Command(0x12, SiteId, 1)).Single()[6].Should().Be(1);
        panel.Respond(MockSamsungMdcResponder.Command(0x12, SiteId, 100)).Single()[6].Should().Be(100);
        panel.Respond(MockSamsungMdcResponder.Command(0x12, SiteId, 101)).Single()[4].Should().Be(MockSamsungMdcResponder.Nak);
        panel.Respond(MockSamsungMdcResponder.Command(0x12, SiteId)).Single()[6].Should().Be(100, "a NAK leaves the level alone");
    }

    [Fact]
    public void Mute_On_Off_Round_Trips()
    {
        var driver = SiteDriver();
        var panel = Panel();

        Exchange(driver, panel, driver.MuteOn());
        driver.IsMuted.Should().BeTrue();
        Exchange(driver, panel, driver.StatusGet());
        driver.IsMuted.Should().BeTrue();
        Exchange(driver, panel, driver.MuteOff());
        driver.IsMuted.Should().BeFalse();
        Exchange(driver, panel, driver.MuteGet());
        driver.IsMuted.Should().BeFalse();
    }

    // ---- Faults ----

    [Fact]
    public void Fault_Ng_Naks_Every_Known_Command_And_Silent_Answers_Nothing()
    {
        var driver = SiteDriver();
        var panel = Panel();
        var commands = new[] { driver.StatusGet(), driver.PowerOn(), driver.InputGeneric(0x21), driver.MuteOn(), driver.VolumeGet() };

        panel.Fault = MockSamsungMdcFault.Ng;
        foreach (var command in commands)
        {
            var reply = panel.Respond(command).Single();
            reply.Should().HaveCount(8);
            reply[4].Should().Be(MockSamsungMdcResponder.Nak);
            reply[5].Should().Be(command[1]);
        }

        panel.Fault = MockSamsungMdcFault.Silent;
        commands.Should().OnlyContain(c => panel.Respond(c).Count == 0);
    }

    [Fact]
    public void Nak_Is_Not_Applied_As_A_Value()
    {
        // A NAK's value byte is an error code (0x01 here); it must not read as "muted" or "power on"
        var driver = SiteDriver();
        var panel = Panel(powerOn: false);
        panel.Fault = MockSamsungMdcFault.Ng;

        Exchange(driver, panel, driver.MuteOn());
        driver.IsMuted.Should().BeFalse();

        // A NAKed status poll is 8 bytes: no value is read and nothing runs past the frame
        Exchange(driver, panel, driver.StatusGet());
        driver.PowerIsOn.Should().BeFalse();
        driver.NaksReceived.Should().Be(2);
        driver.Exceptions.Should().BeEmpty();
    }

    [Fact]
    public void Short_Ack_Is_Ignored_Without_Throwing()
    {
        // A status ACK cut short (valid header/length, too few values) must not index past the frame
        var driver = SiteDriver();
        var shortStatus = new byte[] { 0xAA, 0xFF, 0x00, 0x03, 0x41, 0x00, 0x01, 0x00 };
        shortStatus[7] = MockSamsungMdcResponder.Checksum(shortStatus, 7);

        driver.BytesReceived(shortStatus);

        driver.Exceptions.Should().BeEmpty();
        driver.PowerIsOn.Should().BeFalse();
    }

    [Fact]
    public void Reply_Without_AA_Header_Is_Ignored()
    {
        // A complete frame is parsed before the buffer's header check, so the parser must check AA itself
        var driver = SiteDriver();
        var noHeader = new byte[] { 0x00, 0xFF, SiteId, 0x03, 0x41, 0x13, 0x01, 0x00 };
        noHeader[7] = MockSamsungMdcResponder.Checksum(noHeader, 7);

        driver.BytesReceived(noHeader);

        driver.Exceptions.Should().BeEmpty();
        driver.IsMuted.Should().BeFalse();
    }

    // ---- Driver receive framing (PR #51 territory) ----

    [Fact]
    public void Split_Reply_Is_Reassembled_By_The_Driver()
    {
        var driver = SiteDriver();
        var reply = Panel(configure: o => o.Input = "hdmi4").Respond(driver.StatusGet()).Single();

        driver.BytesReceived(reply.Take(3).ToArray());
        driver.BytesReceived(reply.Skip(3).ToArray());

        driver.Exceptions.Should().BeEmpty();
        driver.CurrentInputNumber.Should().Be(4);
    }

    [Fact]
    public void Coalesced_Replies_Are_All_Parsed()
    {
        // TCP can deliver two replies in one read (e.g. status + LED monitoring when pollLedTemps is on)
        var driver = SiteDriver();
        var panel = Panel(configure: o => o.TemperatureC = 47);
        var both = panel.Respond(driver.StatusGet()).Single().Concat(panel.Respond(driver.LedProductMonitorGet()).Single()).ToArray();

        driver.BytesReceived(both);

        driver.ParsedMessages.Should().HaveCount(2);
        driver.LedTemperatureCelsius.Should().Be(47);
        driver.Buffered.Should().Be(0);
    }

    [Fact]
    public void Coalesced_Reply_Followed_By_Partial_Frame_Keeps_The_Partial()
    {
        var driver = SiteDriver();
        var panel = Panel(configure: o => o.Input = "hdmi3");
        var status = panel.Respond(driver.StatusGet()).Single();
        var input = panel.Respond(driver.InputGet()).Single();

        driver.BytesReceived(status.Concat(input.Take(4)).ToArray());
        driver.ParsedMessages.Should().HaveCount(1);
        driver.Buffered.Should().Be(4);

        driver.CurrentInputNumber = 0;
        driver.BytesReceived(input.Skip(4).ToArray());
        driver.ParsedMessages.Should().HaveCount(2);
        driver.CurrentInputNumber.Should().Be(3);
        driver.Buffered.Should().Be(0);
    }

    [Theory]
    [InlineData(false)] // 5-byte GET echo
    [InlineData(true)]  // 6-byte SET echo
    public void Echoed_Command_Ahead_Of_Reply_Is_Dropped_And_The_Reply_Parsed(bool set)
    {
        // Field condition #51 fixes on main (mock option "echoCommands"): the command comes back ahead of its reply
        var driver = SiteDriver();
        var panel = Panel(configure: o => o.Volume = 55);
        var command = set ? driver.SendVolumeCommand(32768) : driver.VolumeGet();
        var reply = panel.Respond(command).Single();

        driver.BytesReceived(command.Concat(reply).ToArray());

        driver.IgnoredFrames.Should().ContainSingle().Which.Should().Equal(command);
        driver.ParsedMessages.Should().ContainSingle().Which.Should().Equal(reply);
        driver.VolumeLevelForSig.Should().Be((ushort)SamsungMdcDriverCopy.Scale(reply[6], 0, 99, 0, 65535));
        driver.Exceptions.Should().BeEmpty();
    }

    [Fact]
    public void Bad_Checksum_Reply_Is_Dropped_And_The_Next_Frame_Still_Parsed()
    {
        var driver = SiteDriver();
        var panel = Panel();
        var corrupt = panel.Respond(driver.MuteOn()).Single();
        corrupt[corrupt.Length - 1] ^= 0x55;
        var good = panel.Respond(driver.MuteGet()).Single();

        driver.BytesReceived(corrupt.Concat(good).ToArray());

        driver.ParsedMessages.Should().ContainSingle().Which.Should().Equal(good);
        driver.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void Noise_Before_A_Reply_Is_Skipped()
    {
        var driver = SiteDriver();
        var reply = Panel(configure: o => o.Input = "hdmi2").Respond(driver.InputGet()).Single();

        driver.BytesReceived(new byte[] { 0x00, 0x13, 0x0D, 0x0A }.Concat(reply).ToArray());

        driver.CurrentInputNumber.Should().Be(2);
        driver.Buffered.Should().Be(0);
    }

    // ---- Device info and LED monitoring ----

    [Fact]
    public void Led_Monitoring_Temperature_Reaches_The_Driver()
    {
        var driver = SiteDriver();
        Exchange(driver, Panel(configure: o => o.TemperatureC = 47), driver.LedProductMonitorGet());
        driver.LedTemperatureCelsius.Should().Be(47);
    }

    [Fact]
    public void Serial_Number_Reaches_The_Driver()
    {
        var driver = SiteDriver();
        Exchange(driver, Panel(configure: o => o.SerialNumber = "ABC123"), driver.UpdateDeviceInfo()[0]);
        driver.SerialNumber.Should().StartWith("ABC123");
    }

    [Fact]
    public void KnownBug_Software_Version_Parse_Overruns_The_Frame()
    {
        // Array.Copy(message, 6, firmware, 0, message[3]) copies DATA_LEN bytes from offset 6, two past the end
        var driver = SiteDriver();
        Exchange(driver, Panel(), driver.UpdateDeviceInfo()[1]);
        driver.FirmwareVersion.Should().BeNull();
        driver.Exceptions.Should().ContainSingle().Which.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    public void KnownBug_Network_And_Mac_Queries_Use_Sub_Command_As_Command()
    {
        // UpdateDeviceInfo sends AA 82 .. and AA 81 .. ; the spec query is AA 1B id 01 82 cs (0x81 isn't in v14.4c)
        var driver = SiteDriver();
        var panel = Panel();
        var frames = driver.UpdateDeviceInfo();
        frames[2][1].Should().Be(0x82);
        frames[3][1].Should().Be(0x81);
        panel.Respond(frames[2]).Should().BeEmpty();
        panel.Respond(frames[3]).Should().BeEmpty();

        // Even the spec frame wouldn't land: the parser checks message[4] (the 'A') for the sub-command
        Exchange(driver, panel, MockSamsungMdcResponder.Command(0x1B, SiteId, 0x82)).Single()[6].Should().Be(0x82);
        driver.IpAddress.Should().BeNull();
    }

    // ---- Panel-side changes ----

    [Fact]
    public void Panel_Changes_Run_Through_PanelChange_In_Order()
    {
        var panel = Panel();
        var queued = new List<Func<IList<byte[]>>>();
        panel.PanelChange += queued.Add;

        panel.SetVolume(80);
        panel.SetInput("0x25").Should().BeTrue();
        panel.SetInput("nonsense").Should().BeFalse();
        panel.Describe().Should().Contain("volume=20", "nothing applies until the reply thread runs the change");

        queued.Should().HaveCount(2);
        foreach (var change in queued) change().Should().BeEmpty("an MDC panel reports nothing unprompted");
        panel.Describe().Should().Contain("volume=80").And.Contain("displayPort1");
    }
}
