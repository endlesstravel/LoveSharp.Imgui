// based on https://github.com/nem0/LumixEngine/blob/master/external/imgui/imgui_dock.inl
// modified from https://bitbucket.org/duangle/liminal/src/tip/src/liminal/imgui_dock.cpp
// https://github.com/BentleyBlanks/imguiDock/blob/master/imgui_dock.cpp

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Love;
using ImGuiNET;
using ImU32 = System.UInt32;

namespace LoveSharp_Imgui.Thirdparty.Dock
{
    class ImDockMath 
    {
        public static string GenDockId(params object[] args)
        {
            return "dock-id-" + string.Join("-", args);
        }

        public static object GetCurrentWindow()
        {
            return null;
        }

        internal static void IM_ASSERT(bool test)
        {
            if (test == false)
			 throw new Exception("IM_ASSERT error");
        }

        internal static Vector2 Max(Vector2 min_size, Vector2 requested_size)
        {
            throw new NotImplementedException();
        }
    }

    struct ImRect
    {
        private Vector2 min;
        private Vector2 max;

        public ImRect(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public Vector2 Min => min;
        public Vector2 Max => max;

        public bool Contains(Vector2 mouse_pos)
        {
            var ss = max - min;
            return new Love.RectangleF(min, new SizeF(ss.X, ss.Y)).Contains(mouse_pos);
        }

        internal Vector2 GetSize()
        {
            return max - min;
        }
    }

    enum ImGuiDockSlot
    {
        Left,
        Right,
        Top,
        Bottom,
        Tab,
        Float,
        None
    };

    internal class DockContext
    {
        internal enum EndAction_
        {
            None,
            Panel,
            End,
            EndChild
        };


        internal enum Status_
        {
            Status_Docked,
            Status_Float,
            Status_Dragged
        };


        internal class Dock
        {
            public Dock()
            {
                children[0] = children[1] = null;
            }


            public Vector2 getMinSize()
            {
                // !children[0]
                if (children[0] == null) return new Vector2(16, 16 + ImGui.GetTextLineHeightWithSpacing());

                Vector2 s0 = children[0].getMinSize();
                Vector2 s1 = children[1].getMinSize();
                return isHorizontal() ? new Vector2(s0.X + s1.X, Mathf.Max(s0.Y, s1.Y))
                                    : new Vector2(Mathf.Max(s0.X, s1.X), s0.Y + s1.Y);
            }


            public bool isHorizontal() { return children[0].pos.X < children[1].pos.X; }


            public void setParent(Dock dock)
            {
                parent = dock;
                for (Dock tmp = prev_tab; tmp != null; tmp = tmp.prev_tab) tmp.parent = dock;
                for (Dock tmp = next_tab; tmp != null; tmp = tmp.next_tab) tmp.parent = dock;
            }
            
            public Dock getRoot()
            {
                Dock dock = this;
                while (dock.parent != null)
                    dock = dock.parent;
                return dock;
            }


            public Dock getSibling()
            {
                // TODO
                //ImDockMath.IM_ASSERT(parent);
                if (parent.children[0] == getFirstTab()) return parent.children[1];
                return parent.children[0];
            }


            public Dock getFirstTab()
            {
                Dock tmp = this;
                while (tmp.prev_tab != null) tmp = tmp.prev_tab;
                return tmp;
            }


            public void setActive()
            {
                active = true;
                for (Dock tmp = prev_tab; tmp != null; tmp = tmp.prev_tab) tmp.active = false;
                for (Dock tmp = next_tab; tmp != null; tmp = tmp.next_tab) tmp.active = false;
            }


            public bool isContainer() { return children[0] != null; }


            public void setChildrenPosSize(Vector2 _pos, Vector2 _size)
            {
                Vector2 s = children[0].size;
                if (isHorizontal())
                {
                    s.Y = _size.Y;
                    s.X = (float)(int)(
                        _size.X * children[0].size.X / (children[0].size.X + children[1].size.X));
                    if (s.X < children[0].getMinSize().X)
                    {
                        s.X = children[0].getMinSize().X;
                    }
                    else if (_size.X - s.X < children[1].getMinSize().X)
                    {
                        s.X = _size.X - children[1].getMinSize().X;
                    }
                    children[0].setPosSize(_pos, s);

                    s.X = _size.X - children[0].size.X;
                    Vector2 p = _pos;
                    p.X += children[0].size.X;
                    children[1].setPosSize(p, s);
                }
                else
                {
                    s.X = _size.X;
                    s.Y = (float)(int)(
                        _size.Y * children[0].size.Y / (children[0].size.Y + children[1].size.Y));
                    if (s.Y < children[0].getMinSize().Y)
                    {
                        s.Y = children[0].getMinSize().Y;
                    }
                    else if (_size.Y - s.Y < children[1].getMinSize().Y)
                    {
                        s.Y = _size.Y - children[1].getMinSize().Y;
                    }
                    children[0].setPosSize(_pos, s);

                    s.Y = _size.Y - children[0].size.Y;
                    Vector2 p = _pos;
                    p.Y += children[0].size.Y;
                    children[1].setPosSize(p, s);
                }
            }


            public void setPosSize(Vector2 _pos, Vector2 _size)
            {
                size = _size;
                pos = _pos;
                for (Dock tmp = prev_tab; tmp != null; tmp = tmp.prev_tab)
                {
                    tmp.size = _size;
                    tmp.pos = _pos;
                }
                for (Dock tmp = next_tab; tmp != null; tmp = tmp.next_tab)
                {
                    tmp.size = _size;
                    tmp.pos = _pos;
                }

                if (!isContainer()) return;
                setChildrenPosSize(_pos, _size);
            }


            public string label = "";
            public string id;
            public Dock next_tab;
            public Dock prev_tab;
            public Dock[] children = new Dock[2];
            public Dock parent;
            public bool active = true;
            public Vector2 pos;
            public Vector2 size = new Vector2(-1, -1);
            public Status_ status = Status_.Status_Float;
            public int last_frame;
            public int invalid_frames;
            public List<char> location = new List<char>();
            public bool opened;
            public bool first;
        };

        internal List<Dock> m_docks = new List<Dock>();
        internal Vector2 m_drag_offset;
        internal Dock m_current;
        internal Dock m_next_parent;
        internal int m_last_frame;
        internal EndAction_ m_end_action;
        internal Vector2 m_workspace_pos;
        internal Vector2 m_workspace_size;
        internal ImGuiDockSlot m_next_dock_slot = ImGuiDockSlot.Tab;


        Dock getDock(string label, bool opened)
        {
            var id = ImDockMath.GenDockId(label, 0);
            for (int i = 0; i < m_docks.Count; ++i)
            {
                if (m_docks[i].id == id) return m_docks[i];
            }

            Dock new_dock = new Dock();
            m_docks.Add(new_dock);
            new_dock.label = label;
            ImDockMath.IM_ASSERT(new_dock.label != null);
            new_dock.id = id;
            new_dock.setActive();
            new_dock.status = (m_docks.Count == 1)?Status_.Status_Docked: Status_.Status_Float;
            new_dock.pos = new Vector2(0, 0);
            new_dock.size =  ImGui.GetIO().DisplaySize;
            new_dock.opened = opened;
            new_dock.first = true;
            new_dock.last_frame = 0;
            new_dock.invalid_frames = 0;
            new_dock.location.Clear();
            return new_dock;
        }


        void putInBackground()
        {
            //ImGui.IsItemActive();
            //throw new NotImplementedException();
            //ImGuiWindow* win = GetCurrentWindow();
            //ImGuiContext& g = *GImGui;
            //if (g.Windows[0] == win) return;

            //for (int i = 0; i < g.Windows.Size; i++)
            //{
            //    if (g.Windows[i] == win)
            //    {
            //        for (int j = i - 1; j >= 0; --j)
            //        {
            //            g.Windows[j + 1] = g.Windows[j];
            //        }
            //        g.Windows[0] = win;
            //        break;
            //    }
            //}
        }


        void splits()
        {
            if (ImGui.GetFrameCount() == m_last_frame) return;
            m_last_frame = ImGui.GetFrameCount();

            putInBackground();
            
            for (int i = 0; i < m_docks.Count; ++i) {
                Dock dock = m_docks[i];
                if (dock.parent == null && (dock.status == Status_.Status_Docked)) {
                    dock.setPosSize(m_workspace_pos, m_workspace_size);
                }
            }

            ImU32 color = ImGui.GetColorU32(ImGuiCol.Button);
            ImU32 color_hovered = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            var draw_list = ImGui.GetWindowDrawList();
            var io = ImGui.GetIO();
            for (int i = 0; i < m_docks.Count; ++i)
            {
                Dock dock = m_docks[i];
                if (!dock.isContainer()) continue;

                ImGui.PushID(i);
                if (!ImGui.IsMouseDown(0)) dock.status = Status_.Status_Docked;
                
                Vector2 pos0 = dock.children[0].pos;
                Vector2 pos1 = dock.children[1].pos;
                Vector2 size0 = dock.children[0].size;
                Vector2 size1 = dock.children[1].size;
                
                ImGuiMouseCursor cursor;

                Vector2 dsize = new Vector2(0, 0);
                Vector2 min_size0 = dock.children[0].getMinSize();
                Vector2 min_size1 = dock.children[1].getMinSize();
                if (dock.isHorizontal())
                {
                    cursor = ImGuiMouseCursor.ResizeEW;
                    ImGui.SetCursorScreenPos(new Vector2(dock.pos.X + size0.X, dock.pos.Y));
                    ImGui.InvisibleButton("split", new Vector2(3, dock.size.Y));
                    if (dock.status == Status_.Status_Dragged) dsize.X = io.MouseDelta.X;
                    dsize.X = -Mathf.Min(-dsize.X, dock.children[0].size.X - min_size0.X);
                    dsize.X = Mathf.Min(dsize.X, dock.children[1].size.X - min_size1.X);
                    size0 += dsize;
                    size1 -= dsize;
                    pos0 = dock.pos;
                    pos1.X = pos0.X + size0.X;
                    pos1.Y = dock.pos.Y;
                    size0.Y = size1.Y = dock.size.Y;
                    size1.X = Mathf.Max(min_size1.X, dock.size.X - size0.X);
                    size0.X = Mathf.Max(min_size0.X, dock.size.X - size1.X);
                }
                else
                {
                    cursor = ImGuiMouseCursor.ResizeNS;
                    ImGui.SetCursorScreenPos(new Vector2(dock.pos.X, dock.pos.Y + size0.Y - 3));
                    ImGui.InvisibleButton("split", new Vector2(dock.size.X, 3));
                    if (dock.status == Status_.Status_Dragged) dsize.Y = io.MouseDelta.Y;
                    dsize.Y = -Mathf.Min(-dsize.Y, dock.children[0].size.Y - min_size0.Y);
                    dsize.Y = Mathf.Min(dsize.Y, dock.children[1].size.Y - min_size1.Y);
                    size0 += dsize;
                    size1 -= dsize;
                    pos0 = dock.pos;
                    pos1.X = dock.pos.X;
                    pos1.Y = pos0.Y + size0.Y;
                    size0.X = size1.X = dock.size.X;
                    size1.Y = Mathf.Max(min_size1.Y, dock.size.Y - size0.Y);
                    size0.Y = Mathf.Max(min_size0.Y, dock.size.Y - size1.Y);
                }
                dock.children[0].setPosSize(pos0, size0);
                dock.children[1].setPosSize(pos1, size1);

                if (ImGui.IsItemHovered()) {
                    ImGui.SetMouseCursor(cursor);
                }
                
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                {
                    dock.status = Status_.Status_Dragged;
                }

                draw_list.AddRectFilled(
                    ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.IsItemHovered() ? color_hovered : color);
                ImGui.PopID();
            }
        }


        void checkNonexistent()
        {
            int frame_limit = Mathf.Max(0, ImGui.GetFrameCount() - 2);
            for (int i = 0; i < m_docks.Count; ++i)
            {
                Dock dock = m_docks[i];
                if (dock.isContainer()) continue;
                if (dock.status == Status_.Status_Float) continue;
                if (dock.last_frame < frame_limit)
                {
                    ++dock.invalid_frames;
                    if (dock.invalid_frames > 2)
                    {
                        doUndock(dock);
                        dock.status = Status_.Status_Float;
                    }
                    return;
                }
                dock.invalid_frames = 0;
            }
        }


        Dock getDockAt()
        {
            for (int i = 0; i < m_docks.Count; ++i)
            {
                Dock dock = m_docks[i];
                if (dock.isContainer()) continue;
                if (dock.status != Status_.Status_Docked) continue;
                if (ImGui.IsMouseHoveringRect(dock.pos, dock.pos + dock.size, false))
                {
                    return dock;
                }
            }

            return null;
        }


        static ImRect getDockedRect(ImRect rect, ImGuiDockSlot dock_slot)
        {
            Vector2 half_size = rect.GetSize() * 0.5f;
            switch (dock_slot)
            {
                default: return rect;
                case ImGuiDockSlot.Top: return new ImRect(rect.Min, new Vector2(rect.Max.X, rect.Min.Y + half_size.Y));
                case ImGuiDockSlot.Right: return new ImRect(rect.Min + new Vector2(half_size.X, 0), rect.Max);
                case ImGuiDockSlot.Bottom: return new ImRect(rect.Min + new Vector2(0, half_size.Y), rect.Max);
                case ImGuiDockSlot.Left: return new ImRect(rect.Min, new Vector2(rect.Min.X + half_size.X, rect.Max.Y));
            }
        }


        static ImRect getSlotRect(ImRect parent_rect, ImGuiDockSlot dock_slot)
        {
            Vector2 size = parent_rect.Max - parent_rect.Min;
            Vector2 center = parent_rect.Min + size * 0.5f;
            switch (dock_slot)
            {
                default: return new ImRect(center - new Vector2(20, 20), center + new Vector2(20, 20));
                case ImGuiDockSlot.Top: return new ImRect(center + new Vector2(-20, -50), center + new Vector2(20, -30));
                case ImGuiDockSlot.Right: return new ImRect(center + new Vector2(30, -20), center + new Vector2(50, 20));
                case ImGuiDockSlot.Bottom: return new ImRect(center + new Vector2(-20, +30), center + new Vector2(20, 50));
                case ImGuiDockSlot.Left: return new ImRect(center + new Vector2(-50, -20), center + new Vector2(-30, 20));
            }
        }


        static ImRect getSlotRectOnBorder(ImRect parent_rect, ImGuiDockSlot dock_slot)
        {
            Vector2 size = parent_rect.Max - parent_rect.Min;
            Vector2 center = parent_rect.Min + size * 0.5f;
            switch (dock_slot)
            {
                case ImGuiDockSlot.Top:
                    return new ImRect(new Vector2(center.X - 20, parent_rect.Min.Y + 10),
                        new Vector2(center.X + 20, parent_rect.Min.Y + 30));
                case ImGuiDockSlot.Left:
                    return new ImRect(new Vector2(parent_rect.Min.X + 10, center.Y - 20),
                        new Vector2(parent_rect.Min.X + 30, center.Y + 20));
                case ImGuiDockSlot.Bottom:
                    return new ImRect(new Vector2(center.X - 20, parent_rect.Max.Y - 30),
                        new Vector2(center.X + 20, parent_rect.Max.Y - 10));
                case ImGuiDockSlot.Right:
                    return new ImRect(new Vector2(parent_rect.Max.X - 30, center.Y - 20),
                        new Vector2(parent_rect.Max.X - 10, center.Y + 20));
                default:
                    ImDockMath.IM_ASSERT(false);
                    break;
            }
            ImDockMath.IM_ASSERT(false);
            return new ImRect();
        }


        Dock getRootDock()
        {
            for (int i = 0; i < m_docks.Count; ++i)
            {
                if (m_docks[i].parent == null &&
                    (m_docks[i].status == Status_.Status_Docked || m_docks[i].children[0] != null))
                {
                    return m_docks[i];
                }
            }
            return null;
        }


        bool dockSlots(Dock dock, Dock dest_dock, ImRect rect, bool on_border)
        {
            var canvas = ImGui.GetWindowDrawList();
            ImU32 color = ImGui.GetColorU32(ImGuiCol.Button);
            ImU32 color_hovered = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            Vector2 mouse_pos = ImGui.GetIO().MousePos;
            for (int i = 0; i < (on_border ? 4 : 5); ++i)
            {
                ImRect r =
                    on_border ? getSlotRectOnBorder(rect, (ImGuiDockSlot)i) : getSlotRect(rect, (ImGuiDockSlot)i);
                bool hovered = r.Contains(mouse_pos);
                canvas.AddRectFilled(r.Min, r.Max, hovered ? color_hovered : color);
                if (!hovered) continue;

                if (!ImGui.IsMouseDown(0))
                {
                    doDock(dock, dest_dock != null ? dest_dock : getRootDock(), (ImGuiDockSlot)i);
                    return true;
                }
                ImRect docked_rect = getDockedRect(rect, (ImGuiDockSlot)i);
                canvas.AddRectFilled(docked_rect.Min, docked_rect.Max, ImGui.GetColorU32(ImGuiCol.Button));
            }
            return false;
        }


        void handleDrag(Dock dock)
        {
            Dock dest_dock = getDockAt();

            ImGui.Begin("##Overlay",
                ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.AlwaysAutoResize);
            var canvas = ImGui.GetWindowDrawList();

            canvas.PushClipRectFullScreen();

            ImU32 docked_color = ImGui.GetColorU32(ImGuiCol.FrameBg);
            docked_color = (docked_color & 0x00ffFFFF) | 0x80000000;
            dock.pos = ImGui.GetIO().MousePos - m_drag_offset;
            if (dest_dock != null)
            {
                if (dockSlots(dock,
                        dest_dock,
                        new ImRect(dest_dock.pos, dest_dock.pos + dest_dock.size),
                        false))
                {
                    canvas.PopClipRect();
                    ImGui.End();
                    return;
                }
            }
            if (dockSlots(dock, null, new ImRect(m_workspace_pos, m_workspace_pos + m_workspace_size), true))
            {
                canvas.PopClipRect();
                ImGui.End();
                return;
            }
            canvas.AddRectFilled(dock.pos, dock.pos + dock.size, docked_color);
            canvas.PopClipRect();

            if (!ImGui.IsMouseDown(0))
            {
                dock.status = Status_.Status_Float;
                dock.location.Clear();
                dock.setActive();
            }

            ImGui.End();
        }


        void fillLocation(Dock dock)
        {
            if (dock.status == Status_.Status_Float) return;
            Dock tmp = dock;
            while (tmp.parent != null)
            {
                dock.location.Add(getLocationCode(tmp));
                tmp = tmp.parent;
            }

            //for (; i < dock.location.Count && tmp.parent != null; i++)
            //{
            //    dock.location[i] = getLocationCode(tmp);
            //}
            //char* c = dock.location;
            //Dock* tmp = &dock;
            //while (tmp->parent)
            //{
            //    *c = getLocationCode(tmp);
            //    tmp = tmp->parent;
            //    ++c;
            //}
            //*c = 0;
        }


        void doUndock(Dock dock)
        {
            if (dock.prev_tab != null)
                dock.prev_tab.setActive();
            else if (dock.next_tab != null)
                dock.next_tab.setActive();
            else
                dock.active = false;
            Dock container = dock.parent;

            if (container != null)
            {
                Dock sibling = dock.getSibling();
                if (container.children[0] == dock)
                {
                    container.children[0] = dock.next_tab;
                }
                else if (container.children[1] == dock)
                {
                    container.children[1] = dock.next_tab;
                }

                bool remove_container = container.children[0] == null || container.children[1] == null;
                if (remove_container)
                {
                    if (container.parent != null)
                    {
                        var child = container.parent.children[0] == container
                                        ? container.parent.children[0]
                                        : container.parent.children[1];
                        child = sibling;
                        child.setPosSize(container.pos, container.size);
                        child.setParent(container.parent);
                    }
                    else
                    {
                        if (container.children[0] != null)
                        {
                            container.children[0].setParent(null);
                            container.children[0].setPosSize(container.pos, container.size);
                        }
                        if (container.children[1] != null)
                        {
                            container.children[1].setParent(null);
                            container.children[1].setPosSize(container.pos, container.size);
                        }
                    }
                    //for (int i = 0; i < m_docks.Count; ++i)
                    //{
                    //    if (m_docks[i] == container)
                    //    {
                    //        //m_docks.erase(m_docks.begin() + i);
                    //        break;
                    //    }
                    //}
                    m_docks.Remove(container);
                    if (container == m_next_parent)
                        m_next_parent = null;
                }
            }
            if (dock.prev_tab != null) dock.prev_tab.next_tab = dock.next_tab;
            if (dock.next_tab != null) dock.next_tab.prev_tab = dock.prev_tab;
            dock.parent = null;
            dock.prev_tab = dock.next_tab = null;
        }


        void drawTabbarListButton(Dock dock)
        {
            if (dock.next_tab == null) return;

            var draw_list = ImGui.GetWindowDrawList();
            if (ImGui.InvisibleButton("list", new Vector2(16, 16)))
            {
                ImGui.OpenPopup("tab_list_popup");
            }
            if (ImGui.BeginPopup("tab_list_popup"))
            {
                Dock tmp = dock;
                while (tmp != null)
                {
                    bool dummy = false;
                    if (ImGui.Selectable(tmp.label, ref dummy))
                    {
                        tmp.setActive();
                        m_next_parent = tmp;
                    }
                    tmp = tmp.next_tab;
                }
                ImGui.EndPopup();
            }

            bool hovered = ImGui.IsItemHovered();
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            Vector2 center = (min + max) * 0.5f;
            ImU32 text_color = ImGui.GetColorU32(ImGuiCol.Text);
            ImU32 color_active = ImGui.GetColorU32(ImGuiCol.FrameBgActive);
            draw_list.AddRectFilled(new Vector2(center.X - 4, min.Y + 3),
                new Vector2(center.X + 4, min.Y + 5),
                hovered ? color_active : text_color);
            draw_list.AddTriangleFilled(new Vector2(center.X - 4, min.Y + 7),
                new Vector2(center.X + 4, min.Y + 7),
                new Vector2(center.X, min.Y + 12),
                hovered ? color_active : text_color);
        }


        bool tabbar(Dock dock, bool close_button)
        {
            float tabbar_height = 2 * ImGui.GetTextLineHeightWithSpacing();
            Vector2 size = new Vector2(dock.size.X, tabbar_height);
            bool tab_closed = false;

            ImGui.SetCursorScreenPos(dock.pos);
            //char[] tmp = new char[20];
            //ImFormatString(tmp, IM_ARRAYSIZE(tmp), "tabs%d", (int)dock.id);
            string tmp = $"tabs{dock.id}";
            if (ImGui.BeginChild(tmp, size, true))
            {
                Dock dock_tab = dock;

                var draw_list = ImGui.GetWindowDrawList();
                ImU32 color = ImGui.GetColorU32(ImGuiCol.FrameBg);
                ImU32 color_active = ImGui.GetColorU32(ImGuiCol.FrameBgActive);
                ImU32 color_hovered = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);
                ImU32 text_color = ImGui.GetColorU32(ImGuiCol.Text);
                float line_height = ImGui.GetTextLineHeightWithSpacing();
                float tab_base = 0;

                drawTabbarListButton(dock);

                while (dock_tab != null)
                {
                    ImGui.SameLine(0, 15);

                    Vector2 txt_size = new Vector2(ImGui.CalcTextSize(dock_tab.label).X, line_height);
                    if (ImGui.InvisibleButton(dock_tab.label, txt_size))
                    {
                        dock_tab.setActive();
                        m_next_parent = dock_tab;
                    }

                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging())
                    {
                        m_drag_offset = ImGui.GetMousePos() - dock_tab.pos;
                        doUndock(dock_tab);
                        dock_tab.status = Status_.Status_Dragged;
                    }

                    bool hovered = ImGui.IsItemHovered();
                    Vector2 pos = ImGui.GetItemRectMin();
                    if (dock_tab.active && close_button)
                    {
                        txt_size.X += 16 + ImGui.GetStyle().ItemSpacing.X;
                        ImGui.SameLine();
                        tab_closed = ImGui.InvisibleButton("close", new Vector2(16, 16));
                        Vector2 center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                        draw_list.AddLine(
                            center + new Vector2(-3.5f, -3.5f), center + new Vector2(3.5f, 3.5f), text_color);
                        draw_list.AddLine(
                            center + new Vector2(3.5f, -3.5f), center + new Vector2(-3.5f, 3.5f), text_color);
                    }
                    tab_base = pos.Y;
                    draw_list.PathClear();
                    draw_list.PathLineTo(pos + new Vector2(-15, txt_size.Y));
                    draw_list.PathBezierCurveTo(
                        pos + new Vector2(-10, txt_size.Y), pos + new Vector2(-5, 0), pos + new Vector2(0, 0), 10);
                    draw_list.PathLineTo(pos + new Vector2(txt_size.X, 0));
                    draw_list.PathBezierCurveTo(pos + new Vector2(txt_size.X + 5, 0),
                        pos + new Vector2(txt_size.X + 10, txt_size.Y),
                        pos + new Vector2(txt_size.X + 15, txt_size.Y),
                        10);
                    draw_list.PathFillConvex(
                        hovered ? color_hovered : (dock_tab.active ? color_active : color));
                    draw_list.AddText(pos + new Vector2(0, 1), text_color, dock_tab.label);

                    dock_tab = dock_tab.next_tab;
                }
                Vector2 cp = new Vector2(dock.pos.X, tab_base + line_height);
                draw_list.AddLine(cp, cp + new Vector2(dock.size.X, 0), color);
            }
            ImGui.EndChild();
            return tab_closed;
        }


        static void setDockPosSize(Dock dest, Dock dock, ImGuiDockSlot dock_slot, Dock container)
        {
            ImDockMath.IM_ASSERT(dock.prev_tab == null && dock.next_tab == null && dock.children[0] == null && dock.children[1] == null);

            dest.pos = container.pos;
            dest.size = container.size;
            dock.pos = container.pos;
            dock.size = container.size;

            switch (dock_slot)
            {
                case ImGuiDockSlot.Bottom:
                    dest.size.Y *= 0.5f;
                    dock.size.Y *= 0.5f;
                    dock.pos.Y += dest.size.Y;
                    break;
                case ImGuiDockSlot.Right:
                    dest.size.X *= 0.5f;
                    dock.size.X *= 0.5f;
                    dock.pos.X += dest.size.X;
                    break;
                case ImGuiDockSlot.Left:
                    dest.size.X *= 0.5f;
                    dock.size.X *= 0.5f;
                    dest.pos.X += dock.size.X;
                    break;
                case ImGuiDockSlot.Top:
                    dest.size.Y *= 0.5f;
                    dock.size.Y *= 0.5f;
                    dest.pos.Y += dock.size.Y;
                    break;
                default: ImDockMath.IM_ASSERT(false); break;
            }
            dest.setPosSize(dest.pos, dest.size);

            if (container.children[1].pos.X < container.children[0].pos.X ||
                container.children[1].pos.Y < container.children[0].pos.Y)
            {
                Dock tmp = container.children[0];
                container.children[0] = container.children[1];
                container.children[1] = tmp;
            }
        }


        void doDock(Dock dock, Dock dest, ImGuiDockSlot dock_slot)
        {
            ImDockMath.IM_ASSERT(dock.parent == null);
            if (dest == null)
            {
                dock.status = Status_.Status_Docked;
                dock.setPosSize(m_workspace_pos, m_workspace_size);
            }
            else if (dock_slot == ImGuiDockSlot.Tab)
            {
                Dock tmp = dest;
                while( tmp.next_tab != null)    tmp = tmp.next_tab;

                bool inLinkList(Dock linkList, Dock checkNode)
                {
                    bool isLinkNode = ( linkList == checkNode );

                    Dock temp = linkList;
                    while( !isLinkNode  && temp.prev_tab != null)
                    {
                        temp = temp.prev_tab;

                        isLinkNode = ( temp == checkNode );
                    }

                    temp = linkList;

                    while( !isLinkNode && temp.next_tab != null)
                    {
                        temp = temp.next_tab;

                        isLinkNode = ( temp == checkNode );
                    }

                    return isLinkNode;
                };

                if( !inLinkList( dest , dock ) )
                {
                    tmp.next_tab = dock;
                    dock.prev_tab = tmp;
                    dock.size = tmp.size;
                    dock.pos = tmp.pos;
                    dock.parent = dest.parent;
                    dock.status = Status_.Status_Docked;
                }
            }
            else if (dock_slot == ImGuiDockSlot.None)
            {
                dock.status = Status_.Status_Float;
            }
            else
            {
                Dock container = new Dock();
                m_docks.Add(container);
                container.children[0] = dest.getFirstTab();
                container.children[1] = dock;
                container.next_tab = null;
                container.prev_tab = null;
                container.parent = dest.parent;
                container.size = dest.size;
                container.pos = dest.pos;
                container.status = Status_.Status_Docked;
                container.label = "";

                if (dest.parent == null)
                {
                }
                else if (dest.getFirstTab() == dest.parent.children[0])
                {
                    dest.parent.children[0] = container;
                }
                else
                {
                    dest.parent.children[1] = container;
                }

                dest.setParent(container);
                dock.parent = container;
                dock.status = Status_.Status_Docked;

                setDockPosSize(dest, dock, dock_slot, container);
            }
            dock.setActive();
        }


        void rootDock(Vector2 pos, Vector2 size)
        {
            Dock root = getRootDock();
            if (root == null) return;

            Vector2 min_size = root.getMinSize();
            Vector2 requested_size = size;
            root.setPosSize(pos, ImDockMath.Max(min_size, requested_size));
        }

        static ImGuiDockSlot getSlotFromLocationCode(char code)
        {
            switch (code)
            {
                case '1': return ImGuiDockSlot.Left;
                case '2': return ImGuiDockSlot.Top;
                case '3': return ImGuiDockSlot.Bottom;
                default: return ImGuiDockSlot.Right;
            }
        }


        static char getLocationCode(Dock dock)
        {
            if (dock == null) return '0';

            if (dock.parent.isHorizontal())
            {
                if (dock.pos.X < dock.parent.children[0].pos.X) return '1';
                if (dock.pos.X < dock.parent.children[1].pos.X) return '1';
                return '0';
            }
            else
            {
                if (dock.pos.Y < dock.parent.children[0].pos.Y) return '2';
                if (dock.pos.Y < dock.parent.children[1].pos.Y) return '2';
                return '3';
            }
        }


        void tryDockToStoredLocation(Dock dock)
        {
            if (dock.status == Status_.Status_Docked) return;
            if (dock.location.Count == 0) return;

            Dock tmp = getRootDock();
            if (tmp == null) return;

            Dock prev = null;
            //char* c = dock.location + strlen(dock.location) - 1;
            int location_idx = dock.location.Count - 1;
            while (location_idx >= 0 && tmp != null)
            {
                prev = tmp;
                char c = dock.location[location_idx];
                tmp = (c == getLocationCode(tmp.children[0])) ? tmp.children[0] : tmp.children[1];
                if (tmp != null) --location_idx;
            }
            doDock(dock, tmp != null ? tmp : prev, tmp != null ? ImGuiDockSlot.Tab : getSlotFromLocationCode(dock.location[location_idx]));

            //if (dock.status == Status_.Status_Docked) return;
            //if (dock.location[0] == 0) return;

            //Dock tmp = getRootDock();
            //if (tmp == null) return;

            //Dock prev = null;
            //char* c = dock.location + strlen(dock.location) - 1;
            //while (c >= dock.location && tmp)
            //{
            //    prev = tmp;
            //    tmp = (*c == getLocationCode(tmp.children[0])) ? tmp.children[0] : tmp.children[1];
            //    if(tmp) --c;
            //}
            //doDock(dock, tmp ? tmp : prev, tmp ? ImGuiDockSlot.Tab : getSlotFromLocationCode(*c));
        }


        internal bool begin(string label, bool show_close_button, ref bool opened, ImGuiWindowFlags extra_flags)
        {
            ImGuiDockSlot next_slot = m_next_dock_slot;
            m_next_dock_slot = ImGuiDockSlot.Tab;
            Dock dock = getDock(label, !opened);

            if (!dock.opened && (!opened)) tryDockToStoredLocation(dock);
            dock.last_frame = ImGui.GetFrameCount();
            if (dock.label != label)
            {
                dock.label = null;
                dock.label = label;
            }

            m_end_action = EndAction_.None;

            bool prev_opened = dock.opened;
            bool first = dock.first;
            if (dock.first && opened) opened = dock.opened;
            dock.first = false;
            if (!opened)
            {
                if (dock.status != Status_.Status_Float)
                {
                    fillLocation(dock);
                    doUndock(dock);
                    dock.status = Status_.Status_Float;
                }
                dock.opened = false;
                return false;
            }
            dock.opened = true;

            checkNonexistent();
            
            if (first || (prev_opened != dock.opened)) {
                Dock root = m_next_parent != null ? m_next_parent : getRootDock();
                if (root != null && (dock != root) && dock.parent == null) {
                    doDock(dock, root, next_slot);
                }
                m_next_parent = dock;
            }
            
            m_current = dock;
            if (dock.status == Status_.Status_Dragged) handleDrag(dock);

            bool is_float = dock.status == Status_.Status_Float;

            if (is_float)
            {
                ImGui.SetNextWindowPos(dock.pos);
                ImGui.SetNextWindowSize(dock.size);
                //bool ret = ImGui.Begin(label,
                //    opened,
                //    dock.size,
                //    -1.0f,
                //    ImGuiWindowFlags.NoCollapse /*| ImGuiWindowFlags.ShowBorders*/ | extra_flags); // ImGuiWindowFlags.ShowBorders not used in new version of ImGui

                bool ret_1 = ImGui.Begin(label,
                    ImGuiWindowFlags.NoCollapse | extra_flags); // ImGuiWindowFlags.ShowBorders not used in new version of ImGui

                m_end_action = EndAction_.End;
                dock.pos = ImGui.GetWindowPos();
                dock.size = ImGui.GetWindowSize();

                //if (g.ActiveId == GetCurrentWindow().GetID("#MOVE") && g.IO.MouseDown[0]) // 移动位置 drag
                //{
                //    m_drag_offset = ImGui.GetMousePos() - dock.pos;
                //    doUndock(dock);
                //    dock.status = Status_.Status_Dragged;
                //}
                return ret_1;
            }

            if (!dock.active && dock.status != Status_.Status_Dragged) return false;

            //beginPanel();

            m_end_action = EndAction_.EndChild;
            splits();

            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
            float tabbar_height = ImGui.GetTextLineHeightWithSpacing();
            if (tabbar(dock.getFirstTab(), show_close_button))
            {
                fillLocation(dock);
                opened = false;
            }
            Vector2 pos = dock.pos;
            Vector2 size = dock.size;
            pos.Y += tabbar_height + ImGui.GetStyle().WindowPadding.Y;
            size.Y -= tabbar_height + ImGui.GetStyle().WindowPadding.Y;

            ImGui.SetCursorScreenPos(pos);
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse |
                                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus |
                                    extra_flags;
            bool ret = ImGui.BeginChild(label, size, true, flags);
            ImGui.PopStyleColor();
            
            return ret;
        }


        internal void end()
        {
            m_current = null;
            if (m_end_action != EndAction_.None) {
                if (m_end_action == EndAction_.End)
                {
                    ImGui.End();
                }
                else if (m_end_action == EndAction_.EndChild)
                {
                    ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                }
                //endPanel();
            }
        }


        internal void debugWindow() {
            //ImGui.SetNextWindowSize(new Vector2(300, 300));
            string GenDockerName(Dock dock) => dock?.label?.ToString();
            if (ImGui.Begin("Dock Debug Info")) {
                for (int i = 0; i < m_docks.Count; ++i) {
                    if (ImGui.TreeNode($"Dock {i} ({GenDockerName(m_docks[i])})")) {
                        Dock dock = m_docks[i];
                        ImGui.Text($"pos=({dock.pos}) size=({dock.size})");
                        ImGui.Text($"parent ={GenDockerName(dock.parent)}\n");
                        ImGui.Text($"isContainer() == {dock.isContainer()}\n");
                        ImGui.Text($"status = {dock.status}\n");
                        ImGui.TreePop();
                    }            
                }
                
            }
            ImGui.End();
        }
    }

    public static class DockWindow
    {

        // --------------------------------------Wrap Function------------------------------------------

        static Dictionary<string , DockContext> g_docklist = new Dictionary<string, DockContext>();

        static int getDockIndex(DockContext context , DockContext.Dock dock )
        {
            if(dock == null) return -1;

            for(int i = 0; i < context.m_docks.Count; ++i)
            {
                if(dock == context.m_docks[i]) 
                    return i;
            }

            ImDockMath.IM_ASSERT(false);
            return -1;
        }

        internal static DockContext.Dock getDockByIndex(DockContext context, int idx )
        { 
            if( idx >= 0 && idx < context.m_docks.Count )
            {
                return context.m_docks[idx];
            }
            return null;
        }

        //struct readHelper
        //{
        //    DockContext context = null;
        //    DockContext.Dock dock = null;
        //};
        //static readHelper rhelper = new readHelper();

        //static void* readOpen(ImGuiContext* ctx, ImGuiSettingsHandler handler, string name)
        //{
        //    static std::string context_panel = "";

        //    rhelper.context = null;
        //    rhelper.dock    = null;

        //    std::string tag(name);

        //    if( tag.substr( 0 , 6 ) == "panel:" )
        //    {
        //        context_panel = tag.substr( 6 );
        //    }
        //    // specific size of docks
        //    else if(tag.substr(0, 5) == "Size:")
        //    {
        //        DockContext& context = g_docklist[context_panel.c_str()];

        //        std::string size = tag.substr( 5 );
        //        int dockSize = atoi( size.c_str() );

        //        for( int i = 0; i < dockSize; i++ )
        //        {
        //            DockContext::Dock new_dock = new Dock();
        //            context.m_docks.Add( new_dock );
        //        }

        //        return (void*)NULL;
        //    }
        //    // specific index of dock
        //    else if(tag.substr(0, 5) == "Dock:")
        //    {
        //        if( g_docklist.find( context_panel.c_str() ) != g_docklist.end() )
        //        {
        //            DockContext& context = g_docklist[context_panel.c_str()];

        //            std::string indexStr = tag.substr( 5 );
        //            int index = atoi( indexStr.c_str() );
        //            if( index >= 0 && index < ( int )context.m_docks.Count )
        //            {
        //                rhelper.dock = context.m_docks[index];
        //                rhelper.context = &context;
        //            }
        //        }
        //    }

        //    return (void*) &rhelper;
        //}

        //static void readLine(ImGuiContext* ctx, ImGuiSettingsHandler* handler, void* entry, string line_start)
        //{
        //    readHelper* userdata = ( readHelper* )entry;

        //    if( userdata )
        //    {
        //        int active, opened, status;
        //        int x, y, size_x, size_y;
        //        int prev, next, child0, child1, parent;
        //        char label[64], location[64];

        //        if(sscanf(line_start, "label=%[^\n^\r]", label) == 1)
        //        {
        //            userdata.dock.label = ImStrdup(label);
        //            userdata.dock.id = ImDockMath.ImHash( userdata.dock.label, 0);
        //        }
        //        else if(sscanf(line_start, "x=%d", &x) == 1)
        //        {
        //            userdata.dock.pos.X = ( float )x;
        //        }
        //        else if(sscanf(line_start, "y=%d", &y) == 1)
        //        {
        //            userdata.dock.pos.Y = ( float )y;
        //        }
        //        else if(sscanf(line_start, "size_x=%d", &size_x) == 1)
        //        {
        //            userdata.dock.size.X = ( float )size_x;
        //        }
        //        else if(sscanf(line_start, "size_y=%d", &size_y) == 1)
        //        {
        //            userdata.dock.size.Y = ( float )size_y;
        //        }
        //        else if(sscanf(line_start, "active=%d", &active) == 1)
        //        {
        //            userdata.dock.active = (bool) active;
        //        }
        //        else if(sscanf(line_start, "opened=%d", &opened) == 1)
        //        {
        //            userdata.dock.opened = (bool) opened;
        //        }
        //        else if(sscanf(line_start, "location=%[^\n^\r]", location) == 1)
        //        {
        //            strcpy( userdata.dock.location, location);
        //        }
        //        else if(sscanf(line_start, "status=%d", &status) == 1)
        //        {
        //            userdata.dock.status = (DockContext::Status_) status;
        //        }
        //        else if(sscanf(line_start, "prev=%d", &prev) == 1)
        //        {
        //            userdata.dock.prev_tab = getDockByIndex( *( userdata.context ) , prev );
        //        }
        //        else if(sscanf(line_start, "next=%d", &next) == 1)
        //        {
        //            userdata.dock.next_tab = getDockByIndex( *( userdata.context ) , next );
        //        }
        //        else if(sscanf(line_start, "child0=%d", &child0) == 1)
        //        {
        //            userdata.dock.children[0] = getDockByIndex( *( userdata.context ) , child0 );
        //        }
        //        else if(sscanf(line_start, "child1=%d", &child1) == 1)
        //        {
        //            userdata.dock.children[1] = getDockByIndex( *( userdata.context ) , child1 );
        //        }
        //        else if(sscanf(line_start, "parent=%d", &parent) == 1)
        //        {
        //            userdata.dock.parent = getDockByIndex( *( userdata.context ) , parent );
        //        }
        //    }
        //}

        //static void writeAll(ImGuiContext* ctx, ImGuiSettingsHandler* handler, ImGuiTextBuffer* buf)
        //{
        //    int totalDockNum = 0;
        //    for( const auto& iter : g_docklist )
        //    {
        //        const DockContext& context = iter.second;
        //        totalDockNum += context.m_docks.Count;
        //    }

        //    // Write a buffer
        //    buf.reserve( buf.size() + totalDockNum * sizeof( DockContext::Dock ) + 32 * ( totalDockNum + ( int )g_docklist.size() * 2 ) );

        //    // output size
        //    for( const auto& iter : g_docklist )
        //    {
        //        const DockContext& context = iter.second;

        //        buf.appendf( "[%s][panel:%s]\n" , handler.TypeName , iter.first.c_str() );
        //        buf.appendf( "[%s][Size:%d]\n" , handler.TypeName , ( int )context.m_docks.Count );

        //        for( int i = 0 , docksize = context.m_docks.Count; i < docksize; i++ )
        //        {
        //            const DockContext::Dock d = context.m_docks[i];

        //            // some docks invisible but do exist
        //            buf.appendf( "[%s][Dock:%d]\n" , handler.TypeName , i );
        //            buf.appendf( "label=%s\n" , d.label );
        //            buf.appendf( "x=%d\n" , ( int )d.pos.X );
        //            buf.appendf( "y=%d\n" , ( int )d.pos.Y );
        //            buf.appendf( "size_x=%d\n" , ( int )d.size.X );
        //            buf.appendf( "size_y=%d\n" , ( int )d.size.Y );
        //            buf.appendf( "active=%d\n" , ( int )d.active );
        //            buf.appendf( "opened=%d\n" , ( int )d.opened );
        //            buf.appendf( "location=%s\n" , d.location );
        //            buf.appendf( "status=%d\n" , ( int )d.status );
        //            buf.appendf( "prev=%d\n" , ( int )getDockIndex( context , d.prev_tab ) );
        //            buf.appendf( "next=%d\n" , ( int )getDockIndex( context , d.next_tab ) );
        //            buf.appendf( "child0=%d\n" , ( int )getDockIndex( context , d.children[0] ) );
        //            buf.appendf( "child1=%d\n" , ( int )getDockIndex( context , d.children[1] ) );
        //            buf.appendf( "parent=%d\n" , ( int )getDockIndex( context , d.parent ) );
        //        }
        //    }
        //}


        // ----------------------------------------------API-------------------------------------------------
        internal static void ShutdownDock()
        {
            foreach (var iter in g_docklist)
            {
                DockContext context = iter.Value;

                for( int k = 0 , dock_count = ( int )context.m_docks.Count; k < dock_count; k++ )
                {
                    context.m_docks[k] = null;
                }
            }
            g_docklist.Clear();
        }

        internal static void SetNextDock(string panel , ImGuiDockSlot slot) {
            if (g_docklist.TryGetValue(panel, out var context))
            {
                context.m_next_dock_slot = slot;
            }
        }

        public static string CurrentDockspaceName { internal set; get; } = null;

        internal static bool BeginDockspace(string dockspaceName)
        {
            ImGui.Begin(dockspaceName);
            ImDockMath.IM_ASSERT(dockspaceName != null);
            CurrentDockspaceName = dockspaceName;


            if( CurrentDockspaceName == null )    return false;
        
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar;
            //char child_name[1024];
            //sprintf( child_name , "##%s" , CurrentDockspaceName );
            var child_name = $"##{CurrentDockspaceName}";
            bool result = ImGui.BeginChild( child_name , new Vector2( 0 , 0 ) , false , flags );

            DockContext dockContext  = null;
            if (!g_docklist.TryGetValue(CurrentDockspaceName, out dockContext))
            {
                dockContext = new DockContext();
                g_docklist[CurrentDockspaceName] = dockContext;
            }

            dockContext.m_workspace_pos = ImGui.GetWindowPos();
            dockContext.m_workspace_size = ImGui.GetWindowSize();

            return result;
        }

        internal static void EndDockspace()
        {
            ImGui.EndChild();
            ImGui.End();
            CurrentDockspaceName = null;
        }

        internal static bool BeginDock(string label)
        {
            bool ttee = true;
            return BeginDock(label, ref ttee);
        }

        internal static bool BeginDock(string label, ref bool opened, bool show_close_button = true, ImGuiWindowFlags extra_flags = ImGuiWindowFlags.None)
        {
            ImDockMath.IM_ASSERT( CurrentDockspaceName != null);

            if( CurrentDockspaceName == null )    return false;

            if(g_docklist.TryGetValue(CurrentDockspaceName, out var context) )
            {
                return context.begin($"{label}##{CurrentDockspaceName}", show_close_button, ref opened , extra_flags);
            }
        
            return false;
        }


        internal static void EndDock()
        {
            ImDockMath.IM_ASSERT( CurrentDockspaceName != null);

            if( CurrentDockspaceName == null )    return;

            if (g_docklist.TryGetValue(CurrentDockspaceName, out var context))
            {
                context.end();
            }
        }

        internal static void DockDebugWindow(string dock_panel)
        {
            if (g_docklist.TryGetValue(dock_panel, out var context))
            {
                context.debugWindow();
            }
        }

        internal  static void InitDock()
        {
            //ImGuiContext& g = *GImGui;
            //ImGuiSettingsHandler ini_handler;
            //ini_handler.TypeName = "Dock";
            //ini_handler.TypeHash = ImDockMath.ImHash("Dock", 0, 0);
            //ini_handler.ReadOpenFn = readOpen;
            //ini_handler.ReadLineFn = readLine;
            //ini_handler.WriteAllFn = writeAll;
            //g.SettingsHandlers.push_front(ini_handler);
        }
    }
}
