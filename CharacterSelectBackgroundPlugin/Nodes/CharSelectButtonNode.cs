using CharacterSelectBackgroundPlugin.Utility;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using System.Numerics;

namespace CharacterSelectBackgroundPlugin.Nodes
{
    internal class CharSelectButtonNode : NodeBase<AtkResNode>
    {
        CollisionNode collisionNode;
        NineGridNode borderNode;
        NineGridNode highlightBorderNode;
        TextNode textNode;

        public CharSelectButtonNode(uint baseId) : base(NodeType.Res)
        {
            NodeID = baseId;
            Width = 200;
            Height = 40;
            collisionNode = new()
            {
                NodeID = 100 + baseId,
                X = 8,
                Y = 8,
                Width = 186,
                Height = 26,
                Color = new Vector4(1, 1, 1, 1),
                NodeFlags = NodeFlags.Visible | NodeFlags.AnchorLeft | NodeFlags.AnchorRight | NodeFlags.Enabled | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.Focusable | NodeFlags.EmitsEvents,
                Tooltip = "TestButton",
                OnClick = () => Services.Log.Debug("Clicked")
            };
            collisionNode.AttachNode(this, NodePosition.AsLastChild);
            borderNode = new()
            {
                NodeID = 200 + baseId,
                Width = 200,
                Height = 40,
                TextureWidth = 112,
                TextureHeight = 40,
                NodeFlags = NodeFlags.Visible | NodeFlags.AnchorLeft | NodeFlags.AnchorRight | NodeFlags.Enabled,
                Color = new Vector4(1, 1, 1, 1),
                PartsRenderType = 0,
                TextureCoordinates = new Vector2(0, 0),
                LeftOffset = 20,
                RightOffset = 20
            };

            borderNode.LoadTexture("ui/uld/ButtonE_hr1.tex");
            borderNode.AttachNode(this, NodePosition.AsLastChild);
            highlightBorderNode = new()
            {
                NodeID = 200 + baseId,
                Width = 200,
                Height = 40,
                TextureWidth = 112,
                TextureHeight = 40,
                NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorRight | NodeFlags.Enabled,
                Color = new Vector4(1, 1, 1, 1),
                PartsRenderType = 0,
                TextureCoordinates = new Vector2(0, 40),
                LeftOffset = 20,
                RightOffset = 20
            };

            highlightBorderNode.LoadTexture("ui/uld/ButtonE_hr1.tex");
            highlightBorderNode.AttachNode(this, NodePosition.AsLastChild);
            textNode = new()
            {
                NodeID = 300 + baseId,
                AlignmentType = AlignmentType.Center,
                X = 16,
                Y = 8,
                Width = 168,
                Height = 26,
                FontSize = 14,
                TextColor = new Vector4(1, 1, 1, 1),
                OutlineColor = new Vector4(0, 0x99f / 255f, 1, 1),
                NodeFlags = NodeFlags.Visible,
                TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Edge,
                FontType = FontType.Axis

            };
            textNode.Text = "test";
            textNode.AttachNode(this, NodePosition.AsLastChild);

        }

        public new unsafe void EnableTooltip(IAddonEventManager eventManager, void* addon)
        {
            collisionNode.EnableTooltip(eventManager, addon);
        }
        public new unsafe void EnableOnClick(IAddonEventManager eventManager, void* addon)
        {
            collisionNode.EnableOnClick(eventManager, addon);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                collisionNode.Dispose();
                highlightBorderNode.Dispose();
                borderNode.Dispose();
                textNode.Dispose();
                base.Dispose(disposing);
            }
        }

    }
}
