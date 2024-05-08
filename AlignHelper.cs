using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace noone77521
{
    /// <summary>
    /// AlignHelper.
    /// This plugin is used to align atoms in the scene. 
    /// And support for aligning atoms in the scene to a point on the screen.
    /// It's useful for atom positioning at scene creation.
    /// Or it can be used to keep an atom in the scene in a relative position while it is running.
    /// </summary>
    internal partial class AlignHelper : MVRScript
    {
        private const string _version = "1.20";

        /// <summary>
        /// 默认空字符串
        /// </summary>
        private const string _noneString = "None";

        /// <summary>
        /// 空字符串列表
        /// </summary>
        readonly List<string> _noneStrings = new List<string> { _noneString };

        const bool L_SIDE = false;
        const bool R_SIDE = true;

        #region 变量

        private Atom _containingAtom;

        /// <summary>
        /// 锁定
        /// </summary>
        private JSONStorableBool _locked;

        /// <summary>
        /// 锁定时逐帧更新
        /// </summary>
        private JSONStorableStringChooser _updateModeWhenLockedChooser;

        /// <summary>
        /// 对齐位置X
        /// </summary>
        private JSONStorableBool _enableAlignPositionX;

        /// <summary>
        /// 对齐位置Y
        /// </summary>
        private JSONStorableBool _enableAlignPositionY;

        /// <summary>
        /// 对齐位置Z
        /// </summary>
        private JSONStorableBool _enableAlignPositionZ;

        ///// <summary>
        ///// 对齐位置
        ///// </summary>
        //private JSONStorableBool _enableAlignPosition;

        ///// <summary>
        ///// 对齐角度
        ///// </summary>
        //private JSONStorableBool _enableAlignRotation;

        /// <summary>
        /// 对齐角度X
        /// </summary>
        private JSONStorableBool _enableAlignRotationX;

        /// <summary>
        /// 对齐角度Y
        /// </summary>
        private JSONStorableBool _enableAlignRotationY;

        /// <summary>
        /// 对齐角度Z
        /// </summary>
        private JSONStorableBool _enableAlignRotationZ;

        /// <summary>
        /// 对齐的原子
        /// </summary>
        Atom _alignAtom;

        /// <summary>
        /// 对齐原子选择器
        /// </summary>
        JSONStorableStringChooser _alignAtomJSON;
        /// <summary>
        /// 对齐目标选择器
        /// </summary>
        JSONStorableStringChooser _alignReceiverJSON;

        private JSONStorableFloat _positionOffsetXJSON;
        private JSONStorableFloat _positionOffsetYJSON;
        private JSONStorableFloat _positionOffsetZJSON;

        private JSONStorableFloat _rotationOffsetXJSON;
        private JSONStorableFloat _rotationOffsetYJSON;
        private JSONStorableFloat _rotationOffsetZJSON;

        private JSONStorableFloat _horizontalOffsetJSON;
        private JSONStorableFloat _verticalOffsetJSON;
        private JSONStorableFloat _distanceOffsetJSON;
        private JSONStorableBool _reverseJSON;

        List<UIDynamic> _fixUIList = new List<UIDynamic>();
        List<UIDynamic> _lockTipList = new List<UIDynamic>();

        readonly string _screenAreaName = "Monitor Screen";

        readonly List<string> _screenAreas = new List<string> {
            "None",
            "TopLeft",
            "TopCenter",
            "TopRight",
            "MiddleLeft",
            "MiddleCenter",
            "MiddleRight",
            "BottomLeft",
            "BottomCenter",
            "BottomRight",
        };

        /// <summary>
        /// Update mode when locked
        /// </summary>
        readonly List<string> _updateModes = new List<string> { "Update", "FixUpdate" };

        #endregion

        /// <summary>
        /// 生成对齐原子列表
        /// </summary>
        void MakeAlignAtomList()
        {
            if (_alignAtomJSON == null) return;

            List<string> targetChoices = new List<string>() { _noneString, _screenAreaName };
            var atomUIDs = GetAtomUIDs();
            foreach (string atomUID in atomUIDs)
            {
                var targetAtom = GetAtomById(atomUID);
                if (targetAtom == null)
                    continue;
                if (targetAtom.GetBoolJSONParam("on").val == true && targetAtom.name != "CoreControl")
                {
                    targetChoices.Add(atomUID);
                }
            }

            _alignAtomJSON.choices = targetChoices;
        }

        /// <summary>
        /// 同步原子选项
        /// </summary>
        /// <param name="atomUID"></param>
        void SyncAtomChoices(string atomUID)
        {
            List<string> receiverChoices = new List<string>() { _noneString };
            if (atomUID != null && atomUID != _noneString)
            {
                if (atomUID == _screenAreaName)
                {
                    ShowScreenPointFixUI(true);

                    receiverChoices = _screenAreas.ToList();
                }
                else
                {
                    ShowScreenPointFixUI(false);

                    _alignAtom = GetAtomById(atomUID);
                    if (_alignAtom != null)
                    {
                        FreeControllerV3[] controls = _alignAtom.freeControllers;
                        foreach (var control in controls)
                        {
                            receiverChoices.Add(control.name);
                        }

                        if (atomUID == "[CameraRig]")
                        {
                            receiverChoices.Add("CenterEye");
                            receiverChoices.Add("LeftHand");
                            receiverChoices.Add("RightHand");
                            receiverChoices.Add("NavigationRig");
                        }
                        if (_alignAtom.type == "Person")
                        {
                            receiverChoices.Add("LipTrigger");
                            receiverChoices.Add("MouthTrigger");
                            receiverChoices.Add("ThroatTrigger");
                            receiverChoices.Add("LabiaTrigger");
                            receiverChoices.Add("VaginaTrigger");
                            receiverChoices.Add("DeepVaginaTrigger");
                            receiverChoices.Add("DeeperVaginaTrigger");
                        }
                    }
                }
            }
            else
            {
                ShowScreenPointFixUI(false);
                _alignAtom = null;
            }
            _alignReceiverJSON.choices = receiverChoices;
            _alignReceiverJSON.val = _noneString;
        }
        public override void Init()
        {
            base.Init();

            _containingAtom = containingAtom;

            var alignNow = new JSONStorableAction("Align Now", AlignNow);
            RegisterAction(alignNow);
            var btn = CreateButton("Align Now", R_SIDE);
            btn.height = 100f;
            alignNow.dynamicButton = btn;

            CreateUISpacer(rightSide: R_SIDE);

            _enableAlignPositionX = new JSONStorableBool("Enable Position X Align", true);
            RegisterBool(_enableAlignPositionX);
            CreateToggle(_enableAlignPositionX, R_SIDE);

            _enableAlignPositionY = new JSONStorableBool("Enable Position Y Align", true);
            RegisterBool(_enableAlignPositionY);
            CreateToggle(_enableAlignPositionY, R_SIDE);

            _enableAlignPositionZ = new JSONStorableBool("Enable Position Z Align", true);
            RegisterBool(_enableAlignPositionZ);
            CreateToggle(_enableAlignPositionZ, R_SIDE);

            CreateUISpacer(rightSide: R_SIDE);

            _enableAlignRotationX = new JSONStorableBool("Enable Rotation X Align", true);
            RegisterBool(_enableAlignRotationX);
            CreateToggle(_enableAlignRotationX, R_SIDE);

            _enableAlignRotationY = new JSONStorableBool("Enable Rotation Y Align", true);
            RegisterBool(_enableAlignRotationY);
            CreateToggle(_enableAlignRotationY, R_SIDE);

            _enableAlignRotationZ = new JSONStorableBool("Enable Rotation Z Align", true);
            RegisterBool(_enableAlignRotationZ);
            CreateToggle(_enableAlignRotationZ, R_SIDE);

            CreateUISpacer(rightSide: R_SIDE);

            _reverseJSON = new JSONStorableBool("Reverse", false);
            RegisterBool(_reverseJSON);
            var reverseToggle = CreateToggle(_reverseJSON, R_SIDE);

            CreateUISpacer(rightSide: R_SIDE);

            _locked = new JSONStorableBool("Lock", false);
            _locked.setCallbackFunction = v => { ShowUIList(_lockTipList, v); };
            RegisterBool(_locked);
            CreateToggle(_locked, R_SIDE);

            _updateModeWhenLockedChooser = new JSONStorableStringChooser("Update Mode When Locked", _updateModes, _updateModes.Last(), "Update Mode");
            RegisterStringChooser(_updateModeWhenLockedChooser);
            var updateModePopup = CreatePopup(_updateModeWhenLockedChooser, R_SIDE);
            _lockTipList.Add(updateModePopup);

            CreateUISpacer(50f, R_SIDE);

            var versonText = CreateTextField(new JSONStorableString("Verson", "\r\nAlignHelper:" + _version), R_SIDE);
            versonText.height = 50f;
            versonText.UItext.alignment = TextAnchor.MiddleCenter;

            // --------- left side --------

            _alignAtomJSON = new JSONStorableStringChooser("Target Atom", _noneStrings, _noneString, "Target Atom");
            _alignAtomJSON.setCallbackFunction = SyncAtomChoices;
            CreateScrollablePopup(_alignAtomJSON, L_SIDE);
            RegisterStringChooser(_alignAtomJSON);

            _alignReceiverJSON = new JSONStorableStringChooser("Target Receiver", _noneStrings, _noneString, "Target Receiver");
            CreateScrollablePopup(_alignReceiverJSON, L_SIDE);
            RegisterStringChooser(_alignReceiverJSON);

            _horizontalOffsetJSON = new JSONStorableFloat("Horizontal Offset", 0f, -1000f, 1000f, false);
            _verticalOffsetJSON = new JSONStorableFloat("Vertical Offset", 0f, -1000f, 1000f, false);
            _distanceOffsetJSON = new JSONStorableFloat("Distance Offset", 0f, 0f, 1f, false);

            RegisterFloat(_horizontalOffsetJSON);
            RegisterFloat(_verticalOffsetJSON);
            RegisterFloat(_distanceOffsetJSON);

            var spacer = CreateUISpacer(rightSide: L_SIDE);

            var horizontalSlider = CreateSlider(_horizontalOffsetJSON, L_SIDE);
            horizontalSlider.valueFormat = "F1";
            var verticalSlider = CreateSlider(_verticalOffsetJSON, L_SIDE);
            verticalSlider.valueFormat = "F1";
            var distanceSlider = CreateSlider(_distanceOffsetJSON, L_SIDE);
            distanceSlider.valueFormat = "F4";

            _fixUIList = new List<UIDynamic>
            {
                spacer,
                horizontalSlider,
                verticalSlider,
                distanceSlider
            };

            CreateUISpacer(rightSide: L_SIDE);

            _positionOffsetXJSON = new JSONStorableFloat("Position Offset X", 0f, -2.0f, 2.0f, false);
            CreateSlider(_positionOffsetXJSON, L_SIDE);
            _positionOffsetYJSON = new JSONStorableFloat("Position Offset Y", 0f, -2.0f, 2.0f, false);
            CreateSlider(_positionOffsetYJSON, L_SIDE);
            _positionOffsetZJSON = new JSONStorableFloat("Position Offset Z", 0f, -2.0f, 2.0f, false);
            CreateSlider(_positionOffsetZJSON, L_SIDE);

            CreateUISpacer(rightSide: L_SIDE);

            _rotationOffsetXJSON = new JSONStorableFloat("Rotation Offset X", 0f, -180f, 180f);
            CreateSlider(_rotationOffsetXJSON, L_SIDE);
            _rotationOffsetYJSON = new JSONStorableFloat("Rotation Offset Y", 0f, -180f, 180f);
            CreateSlider(_rotationOffsetYJSON, L_SIDE);
            _rotationOffsetZJSON = new JSONStorableFloat("Rotation Offset Z", 0f, -180f, 180f);
            CreateSlider(_rotationOffsetZJSON, L_SIDE);

            RegisterFloat(_positionOffsetXJSON);
            RegisterFloat(_positionOffsetYJSON);
            RegisterFloat(_positionOffsetZJSON);
            RegisterFloat(_rotationOffsetXJSON);
            RegisterFloat(_rotationOffsetYJSON);
            RegisterFloat(_rotationOffsetZJSON);

            SuperController.singleton.onAtomAddedHandlers = (a) => MakeAlignAtomList();
            SuperController.singleton.onAtomRemovedHandlers = (a) => MakeAlignAtomList();
            SuperController.singleton.onAtomUIDRenameHandlers = (a, b) => MakeAlignAtomList();
            SuperController.singleton.onAtomUIDsChangedHandlers = (a) => MakeAlignAtomList();

            StartCoroutine(InitDeferred());
        }

        /// <summary>
        /// 创建空白区域
        /// </summary>
        /// <param name="height"></param>
        /// <param name="rightSide"></param>
        /// <returns></returns>
        private UIDynamic CreateUISpacer(float height = 10f, bool rightSide = false)
        {
            var spacer = CreateSpacer(rightSide);
            spacer.height = height;
            return spacer;
        }

        protected void Start()
        {
            MakeAlignAtomList();

            ShowUIList(_lockTipList, _locked.val);

            ShowScreenPointFixUI(_alignAtomJSON.val == _screenAreaName);
        }

        void ShowScreenPointFixUI(bool show)
        {
            ShowUIList(_fixUIList, show);
        }

        void ShowUIList(List<UIDynamic> list, bool show)
        {
            foreach (var item in list)
            {
                item.gameObject.SetActive(show);
            }
        }

        protected void FixedUpdate()
        {
            if (enabled && _locked.val && _updateModeWhenLockedChooser.val == "FixedUpdate")
            {
                AlignNow();
            }
        }

        protected void Update()
        {
            if (enabled && _locked.val && _updateModeWhenLockedChooser.val != "FixedUpdate")
            {
                AlignNow();
            }
        }

        private IEnumerator InitDeferred()
        {
            yield return new WaitForEndOfFrame();
            if (!enabled) yield break;
            yield return 0;
            if (!enabled) yield break;
        }

        private void AlignNow()
        {
            if (_alignAtomJSON.val == _noneString || _alignReceiverJSON.val == _noneString)
                return;

            var targetTransform = _containingAtom.mainController.control;

            if (_alignAtomJSON.val == _screenAreaName)
            {
                Vector3 point = Vector3.zero;

                switch (_alignReceiverJSON.val)
                {
                    case "TopLeft":
                        point.y = Screen.height;
                        point.x = 0;
                        break;
                    case "TopCenter":
                        point.y = Screen.height;
                        point.x = Screen.width / 2;
                        break;
                    case "TopRight":
                        point.y = Screen.height;
                        point.x = Screen.width;
                        break;
                    case "MiddleLeft":
                        point.y = Screen.height / 2;
                        point.x = 0;
                        break;
                    case "MiddleCenter":
                        point.y = Screen.height / 2;
                        point.x = Screen.width / 2;
                        break;
                    case "MiddleRight":
                        point.y = Screen.height / 2;
                        point.x = Screen.width;
                        break;
                    case "BottomLeft":
                        point.y = 0;
                        point.x = 0;
                        break;
                    case "BottomCenter":
                        point.y = 0;
                        point.x = Screen.width / 2;
                        break;
                    case "BottomRight":
                        point.y = 0;
                        point.x = Screen.width;
                        break;
                }

                #region 根据开启的位置对齐轴重置位置参数
                if (!_enableAlignPositionX.val)
                {
                    point.x = 0;
                }

                if (!_enableAlignPositionY.val)
                {
                    point.y = 0;
                }

                if (!_enableAlignPositionZ.val)
                {
                    point.z = 0;
                }
                #endregion

                point.x += _horizontalOffsetJSON.val;
                point.y -= _verticalOffsetJSON.val;
                point.z = (float)SuperController.singleton.MonitorCenterCamera.scaledPixelHeight * _distanceOffsetJSON.val / SuperController.singleton.monitorCameraFOV * SuperController.singleton.worldScale;

                var targetScreenPoint = point;

                if (EnableAlignPosition)
                {
                    // 获取世界坐标
                    var position = SuperController.singleton.MonitorCenterCamera.ScreenToWorldPoint(targetScreenPoint);
                    _containingAtom.mainController.control.position = position;
                }

                SetRotation(SuperController.singleton.MonitorCenterCamera.transform);
            }
            else
            {
                if (_alignAtomJSON.val != "[CameraRig]")
                {
                    targetTransform = _alignAtom.GetStorableByID(_alignReceiverJSON.val).transform;
                }
                else
                {
                    switch (_alignReceiverJSON.val)
                    {
                        case "CenterEye":
                            targetTransform = SuperController.singleton.lookCamera.transform;
                            break;
                        case "LeftHand":
                            targetTransform = SuperController.singleton.leftHand.transform;
                            break;
                        case "RightHand":
                            targetTransform = SuperController.singleton.rightHand.transform;
                            break;
                        case "NavigationRig":
                            targetTransform = SuperController.singleton.navigationRig.transform;
                            break;
                    }
                }

                if (EnableAlignPosition)
                {
                    var targetAtomPosition = Vector3.zero;

                    #region 根据开启的位置对齐轴重置位置参数
                    if (_enableAlignPositionX.val)
                    {
                        targetAtomPosition.x = targetTransform.position.x;
                    }

                    if (_enableAlignPositionY.val)
                    {
                        targetAtomPosition.y = targetTransform.position.y;
                    }

                    if (_enableAlignPositionZ.val)
                    {
                        targetAtomPosition.z = targetTransform.position.z;
                    }
                    #endregion

                    _containingAtom.mainController.control.position = targetAtomPosition;
                }

                SetRotation(targetTransform);
            }

            if (_positionOffsetXJSON.val != 0f || _positionOffsetYJSON.val != 0f || _positionOffsetZJSON.val != 0f)
            {
                var adjPosition = new Vector3(_positionOffsetXJSON.val, _positionOffsetYJSON.val, _positionOffsetZJSON.val);
                _containingAtom.mainController.control.position += adjPosition;
            }

            if (_rotationOffsetXJSON.val != 0f || _rotationOffsetYJSON.val != 0f || _rotationOffsetZJSON.val != 0f)
            {
                var rotationOffset = Quaternion.Euler(_rotationOffsetXJSON.val, _rotationOffsetYJSON.val, _rotationOffsetZJSON.val);
                _containingAtom.mainController.control.rotation *= rotationOffset;
            }
        }

        private bool EnableAlignPosition
        {
            get
            {
                return _enableAlignPositionX.val || _enableAlignPositionY.val || _enableAlignPositionZ.val;
            }
        }

        private bool EnableAlignRotation
        {
            get
            {
                return _enableAlignRotationX.val || _enableAlignRotationY.val || _enableAlignRotationZ.val;
            }
        }

        private void SetRotation(Transform targetTransform)
        {
            var targetRotation = targetTransform.eulerAngles;

            if (!_enableAlignRotationX.val)
            {
                targetRotation.x = 0f;
            }

            if (!_enableAlignRotationY.val)
            {
                targetRotation.y = 0f;
            }

            if (!_enableAlignRotationZ.val)
            {
                targetRotation.z = 0f;
            }

            //if (EnableAlignRotation)
            {
                _containingAtom.mainController.control.rotation = Quaternion.Euler(targetRotation);
            }

            if (_reverseJSON.val)
            {
                var reverse = Quaternion.Euler(0f, 180f, 0f);

                _containingAtom.mainController.control.rotation *= reverse;
            }
        }

        protected void OnEnable()
        {
            // 生成对齐原子列表
            MakeAlignAtomList();
        }

        protected void OnDisable()
        {
        }

        protected void OnDestroy()
        {
        }
    }
}