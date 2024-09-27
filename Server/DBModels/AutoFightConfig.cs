using Library;

using MirDB;

namespace Server.DBModels
{
    [UserObject]
    public class AutoFightConfig : DBObject
    {
        private CharacterInfo _Character;
        private AutoSetConf _Slot;
        private MagicType _MagicIndex;
        private int _TimeCount;
        private bool _Enabled;

        [Association("AutoFightLinks")]
        public CharacterInfo Character
        {
            get
            {
                return _Character;
            }
            set
            {
                if (_Character == value)
                    return;
                CharacterInfo character = _Character;
                _Character = value;
                OnChanged((object)character, (object)value, nameof(Character));
            }
        }

        public AutoSetConf Slot
        {
            get
            {
                return _Slot;
            }
            set
            {
                if (_Slot == value)
                    return;
                AutoSetConf slot = _Slot;
                _Slot = value;
                OnChanged((object)slot, (object)value, nameof(Slot));
            }
        }

        public MagicType MagicIndex
        {
            get
            {
                return _MagicIndex;
            }
            set
            {
                if (_MagicIndex == value)
                    return;
                MagicType magicIndex = _MagicIndex;
                _MagicIndex = value;
                OnChanged((object)magicIndex, (object)value, nameof(MagicIndex));
            }
        }

        public int TimeCount
        {
            get
            {
                return _TimeCount;
            }
            set
            {
                if (_TimeCount == value)
                    return;
                int timeCount = _TimeCount;
                _TimeCount = value;
                OnChanged((object)timeCount, (object)value, nameof(TimeCount));
            }
        }

        public bool Enabled
        {
            get
            {
                return _Enabled;
            }
            set
            {
                if (_Enabled == value)
                    return;
                bool enabled = _Enabled;
                _Enabled = value;
                OnChanged((object)enabled, (object)value, nameof(Enabled));
            }
        }

        protected override void OnDeleted()
        {
            Character = (CharacterInfo)null;
            base.OnDeleted();
        }

        public ClientAutoFightLink ToClientInfo()
        {
            return new ClientAutoFightLink()
            {
                Slot = Slot,
                MagicIndex = MagicIndex,
                TimeCount = TimeCount,
                Enabled = Enabled
            };
        }
    }
}
