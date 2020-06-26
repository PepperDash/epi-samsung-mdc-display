﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using PepperDash.Essentials.Core;

namespace PepperDash.Plugin.Display.SamsungMdc
{
	public class DisplayControllerJoinMap : JoinMapBase
	{
		#region Digitals
        /// <summary>
        /// Turns the display off and reports power off feedback
        /// </summary>
        public uint PowerOff { get; set; }
        /// <summary>
        /// Turns the display on and repots power on feedback
        /// </summary>
        public uint PowerOn { get; set; }
        /// <summary>
        /// Indicates that the display device supports two way communication when high
        /// </summary>
        public uint IsTwoWayDisplay { get; set; }
        /// <summary>
        /// Increments the volume while high
        /// </summary>
        public uint VolumeUp { get; set; }
        /// <summary>
        /// Decrements teh volume while high
        /// </summary>
        public uint VolumeDown { get; set; }
        /// <summary>
        /// Toggles the mute state.  Feedback is high when volume is muted
        /// </summary>
        public uint VolumeMute { get; set; }
        /// <summary>
        /// Sets mute state one.
        /// </summary>
        public uint VolumeMuteOn { get; set; }
        /// <summary>
        /// Sets mute state off
        /// </summary>
        public uint VolumeMuteOff { get; set; }
		/// <summary>
		/// Polls for the current max temperature threshold
		/// </summary>
		public uint TemperatureMax { get; set; }
        /// <summary>
        /// Range of digital joins to select inputs and report current input as feedback
        /// </summary>
        public uint InputSelectOffset { get; set; }
        /// <summary>
        /// Range of digital joins to report visibility for input buttons
        /// </summary>
        public uint ButtonVisibilityOffset { get; set; }
        /// <summary>
        /// High if the device is online
        /// </summary>
        public uint IsOnline { get; set; }
        #endregion

        #region Analogs
        /// <summary>
        /// Sets the volume level and reports the current level as feedback 
        /// </summary>
        public uint VolumeLevel { get; set; }
		/// <summary>
		/// Analog join to set the input and report current input as feedback
		/// </summary>
		public uint InputSelect { get; set; }
		/// <summary>
		/// Analog join to report current max temp feedback
		/// </summary>
		public uint TemperatureMaxFb { get; set; }
        #endregion

        #region Serials
        /// <summary>
        /// Reports the name of the display as defined in config as feedback
        /// </summary>
        public uint Name { get; set; }
        /// <summary>
        /// Range of serial joins that reports the names of the inputs as feedback
        /// </summary>
        public uint InputNamesOffset { get; set; }
        #endregion

		/// <summary>
		/// Display controller join map
		/// Some specific adds for Samsung Temperature and Brightness control and feedback
		/// </summary>
		public DisplayControllerJoinMap()
        {
			// Digital			
			PowerOff = 1;
			PowerOn = 2;
			IsTwoWayDisplay = 3;
			VolumeUp = 5;
			VolumeDown = 6;
			VolumeMute = 7;
            VolumeMuteOn = 8;
            VolumeMuteOff = 9;
			IsOnline = 50;

			// Digital offsets (joinStart+offset)
			InputSelectOffset = 10;
			ButtonVisibilityOffset = 40;

			// Analog
			VolumeLevel = 5;
			InputSelect = 11;			

			// Serial
			Name = 1;

			// Serial offsets (joinStart+offset)
			InputNamesOffset = 10;

			// Specific updates
			// Digital
			TemperatureMax = 20;

			// Analog
			TemperatureMaxFb = 20;
        }

		/// <summary>
		/// Offset join numbers using reflection
		/// </summary>
		/// <param name="joinStart"></param>
		public override void OffsetJoinNumbers(uint joinStart)
		{
			var joinOffset = joinStart - 1;
			var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
			foreach (var property in properties)
			{
				property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
			}
		}		
	}
}