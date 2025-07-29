using Independentsoft.Exchange;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PepperDashPluginSamsungMdcDisplay;

namespace PepperDashPluginSamsungMdcDisplay
{
    /// <summary>
    /// Implementation of ISelectableItems interface for Samsung display inputs.
    /// Manages a collection of selectable input items and tracks the currently selected input.
    /// </summary>
    public class SamsungInputs : ISelectableItems<byte>
    {
        private Dictionary<byte, ISelectableItem> _items = new Dictionary<byte, ISelectableItem>();

        /// <summary>
        /// Gets or sets the dictionary of selectable input items, indexed by byte values 
        /// corresponding to Samsung MDC input command values.
        /// </summary>
        /// <value>A dictionary mapping byte input codes to selectable input items.</value>
        public Dictionary<byte, ISelectableItem> Items
        {
            get
            {
                return _items;
            }
            set
            {
                if (_items == value)
                    return;

                _items = value;

                ItemsUpdated?.Invoke(this, null);
            }
        }

        private byte _currentItem;

        /// <summary>
        /// Gets or sets the currently selected input item identified by its byte value.
        /// Setting this property triggers the CurrentItemChanged event.
        /// </summary>
        /// <value>The byte value representing the currently selected input.</value>
        public byte CurrentItem
        {
            get
            {
                return _currentItem;
            }
            set
            {
                if (_currentItem == value)
                    return;

                _currentItem = value;

                CurrentItemChanged?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Occurs when the Items collection is updated.
        /// </summary>
        public event EventHandler ItemsUpdated;
        
        /// <summary>
        /// Occurs when the CurrentItem changes.
        /// </summary>
        public event EventHandler CurrentItemChanged;

    }

    /// <summary>
    /// Represents a selectable input item for Samsung display devices.
    /// Implements ISelectableItem interface and encapsulates input selection behavior.
    /// </summary>
    public class SamsungInput : ISelectableItem
    {
        private bool _isSelected;

        private readonly SamsungMdcDisplayController _parent;

        private Action _inputMethod;

        /// <summary>
        /// Initializes a new instance of the SamsungInput class with the specified parameters.
        /// </summary>
        /// <param name="key">The unique key identifier for this input.</param>
        /// <param name="name">The display name for this input.</param>
        /// <param name="parent">The parent Samsung MDC display controller.</param>
        /// <param name="inputMethod">The action to execute when this input is selected.</param>
        public SamsungInput(string key, string name, SamsungMdcDisplayController parent, Action inputMethod)
        {
            Key = key;
            Name = name;
            _parent = parent;
            _inputMethod = inputMethod;
        }

        /// <summary>
        /// Gets the unique key identifier for this input item.
        /// </summary>
        /// <value>A string that uniquely identifies this input.</value>
        public string Key { get; private set; }
        
        /// <summary>
        /// Gets the display name for this input item.
        /// </summary>
        /// <value>A human-readable string representing this input.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Occurs when this input item is updated.
        /// </summary>
        public event EventHandler ItemUpdated;

        /// <summary>
        /// Gets or sets a value indicating whether this input is currently selected.
        /// Setting this property triggers the ItemUpdated event.
        /// </summary>
        /// <value>True if this input is selected; otherwise, false.</value>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value == _isSelected)
                    return;

                _isSelected = value;
                var handler = ItemUpdated;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Selects this input by executing the associated input method action.
        /// This typically sends the appropriate MDC command to switch the display to this input.
        /// </summary>
        public void Select()
        {
            _inputMethod();
        }
    }
}
