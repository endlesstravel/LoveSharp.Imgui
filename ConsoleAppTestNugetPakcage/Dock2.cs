using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Love;
using ImGuiNET;
using ImU32 = System.UInt32;

namespace LoveSharp_Imgui.Thirdparty.Dock2222
{
    public struct DockStruct
    {
        public string Name;
        public bool CloseButton;
        public Vector2 MinSize;
        public Action<Vector2> DrawFunction;
        public DockSlot Slot;
        public float Size;
        public bool Active;
    }

    public class DockContext
    {
        readonly Dockspace sp;

        internal DockContext(Dockspace sp)
        {
            this.sp = sp ?? throw new ArgumentNullException(nameof(sp));
        }

        public void UpdateAndDraw(Vector2 dockspaceSize)
        {
            sp.updateAndDraw(dockspaceSize);
        }
    }

    public static class DockBuilder
    {
        public static DockContext Build(params DockStruct[] defineList)
        {
            var dockspace = new Dockspace();
            foreach (var item in defineList)
            {
                dockspace.dock(new Dock(item.Name, item.CloseButton, item.MinSize, item.DrawFunction), item.Slot, item.Size, item.Active);
            }
            return new DockContext(dockspace);
        }
    }

    public enum DockSlot { Left, Right, Top, Bottom, Tab, None };

    class Container
    {
        public readonly Container[] splits = new Container[2];
        public Container parent = null;
        public Dock activeDock = null;

        public readonly List<Dock> docks = new List<Dock>();

        public bool verticalSplit = false;
        public bool alwaysAutoResize = true;
        public float size = 0;
    };

    class Dock
    {
        public Dock (string dtitle,  bool dcloseButton, Vector2 dminSize, Action<Vector2> ddrawFunction)
		{
			title = dtitle;
			closeButton = dcloseButton;
			minSize = dminSize;
			drawFunction = ddrawFunction;
		}

        //Container *parent = null;
        public Container container = null;
        public Dockspace redockFrom = null;
        public Dock redockTo = null;

        public string title;
        public DockSlot dockSlot = DockSlot.Tab;
        public DockSlot redockSlot = DockSlot.None;
        public bool closeButton = true;
        public bool undockable = false;
        public bool draging = false;
        public Vector2 lastSize;
        public Vector2 minSize;

        public Action<Vector2> drawFunction;
        public Func<bool> onCloseFunction;
	}

    class Dockspace
    {
        public bool dock(Dock dock, DockSlot dockSlot, float size = 0, bool active = false)
        {
            return dockWith(dock, null, dockSlot, size, active);
        }
        public bool dockWith(Dock dock, Dock dockTo, DockSlot dockSlot, float size = 0, bool active = false)
        {

            if (dock == null)
                return false;

            Container currentContainer = m_container;

            if (dockTo != null)
            {
                if (dockSlot == DockSlot.Tab)
                {
                    dockTo.container.activeDock = active ? dock : currentContainer.splits[0].activeDock != null ? currentContainer.splits[0].activeDock : dock;
                    dockTo.container.docks.Add(dock);
                    dock.container = dockTo.container;
                    return true;
                }
                else
                {
                    m_containers.Add(new Container { });
                    var newContainer = m_containers[m_containers.Count - 1];
                    newContainer.parent = dockTo.container.parent;
                    newContainer.splits[0] = dockTo.container;
                    newContainer.size = dockTo.container.size;
                    //if (size)
                    //	newContainer.alwaysAutoResize = false;
                    dockTo.container.size = 0;
                    if (dockTo.container.parent.splits[0] == dockTo.container)
                        dockTo.container.parent.splits[0] = newContainer;
                    else dockTo.container.parent.splits[1] = newContainer;
                    //dockTo.container.parent = newContainer;
                    dockTo.container = newContainer.splits[0];
                    dockTo.container.parent = newContainer;
                    currentContainer = newContainer;
                }
            }

            Container childContainer = null;
            if (currentContainer.splits[0] == null || currentContainer.splits[1] == null)
            {
                m_containers.Add(new Container { });
                childContainer = m_containers[m_containers.Count - 1];
            };

            if (currentContainer.splits[0] == null)
            {
                currentContainer.splits[0] = childContainer;
                currentContainer.splits[0].activeDock = active ? dock : currentContainer.splits[0].activeDock != null ? currentContainer.splits[0].activeDock : dock;
                currentContainer.splits[0].docks.Add(dock);
                currentContainer.splits[0].parent = currentContainer;
                currentContainer.splits[0].size = size < 0 ? size * -1 : size;
                dock.container = currentContainer.splits[0];
                dock.container.parent = currentContainer;
            }
            else if (currentContainer.splits[1] == null)
            {
                currentContainer.splits[1] = childContainer;
                Container otherSplit = currentContainer.splits[0];
                if (size > 0)
                {
                    currentContainer.splits[0].alwaysAutoResize = true;
                    currentContainer.splits[0].size = 0;
                    currentContainer.splits[1].size = size;
                    currentContainer.splits[1].alwaysAutoResize = false;
                }
                else if (size == 0) { }
                else
                {
                    currentContainer.splits[0].alwaysAutoResize = false;
                    currentContainer.splits[0].size = size * -1;
                    currentContainer.splits[1].size = 0;
                    currentContainer.splits[1].alwaysAutoResize = true;
                }
                switch (dockSlot)
                {
                    case DockSlot.Left:
                        currentContainer.splits[1] = currentContainer.splits[0];
                        currentContainer.splits[0] = childContainer;
                        currentContainer.verticalSplit = true;
                        break;
                    case DockSlot.Right:
                        currentContainer.verticalSplit = true;
                        break;
                    case DockSlot.Top:
                        currentContainer.splits[1] = currentContainer.splits[0];
                        currentContainer.splits[0] = childContainer;
                        currentContainer.verticalSplit = false;
                        break;
                    case DockSlot.Bottom:
                        currentContainer.verticalSplit = false;
                        break;
                    case DockSlot.Tab:
                        currentContainer.verticalSplit = false;
                        break;
                    case DockSlot.None:
                        break;
                    default:
                        break;
                }
                childContainer.activeDock = active ? dock : childContainer.activeDock != null ? childContainer.activeDock : dock;
                childContainer.docks.Add(dock);
                childContainer.parent = currentContainer;

                //	if (childContainer.parent != null && currentContainer.verticalSplit != childContainer.parent.verticalSplit)
                //		currentContainer.size = otherSplit.size != 0 ? otherSplit.size + otherSplit.size : otherSplit.size;

                dock.container = childContainer;
            }
            else
            {
                return false;
            }

            return true;
        }
        public bool undock(Dock dock)
        {

            if (dock != null)
            {
                if (dock.container.docks.Count > 1)
                {
                    for (int i = 0; i < dock.container.docks.Count; i++)
                    {
                        if (dock.container.docks[i] == dock)
                        {
                            dock.lastSize = dock.container.activeDock.lastSize;
                            //dock.container.docks.erase(dock.container.docks.begin() + i);
                            // Trans:
                            dock.container.docks.RemoveAt(i);
                            if (i != dock.container.docks.Count)
                                dock.container.activeDock = dock.container.docks[i];
                            else dock.container.activeDock = dock.container.docks[i - 1];
                        }
                    }
                }
                else
                {
                    Container toDelete = null, parentToDelete = null;
                    if (dock.container.parent == m_container)
                    {
                        if (m_container.splits[0] == dock.container)
                        {
                            if (m_container.splits[1] != null)
                            {
                                toDelete = m_container.splits[0];
                                if (m_container.splits[1].splits[0] != null)
                                {
                                    parentToDelete = m_container.splits[1];
                                    m_container.splits[0] = m_container.splits[1].splits[0];
                                    m_container.splits[0].parent = m_container;
                                    m_container.splits[0].verticalSplit = false;
                                    m_container.splits[1] = m_container.splits[1].splits[1];
                                    m_container.splits[1].parent = m_container;
                                    m_container.splits[1].parent.verticalSplit = m_container.splits[1].verticalSplit;
                                    m_container.splits[1].verticalSplit = false;
                                }
                                else
                                {
                                    m_container.splits[0] = m_container.splits[1];
                                    m_container.splits[1] = null;
                                    m_container.splits[0].size = 0;
                                    m_container.splits[0].verticalSplit = false;
                                    m_container.splits[0].parent.verticalSplit = false;
                                }
                            }
                            else return false;
                        }
                        else
                        {
                            toDelete = m_container.splits[1];
                            m_container.splits[1] = null;
                        }
                    }
                    else
                    {
                        parentToDelete = dock.container.parent;
                        if (dock.container.parent.splits[0] == dock.container)
                        {
                            toDelete = dock.container.parent.splits[0];
                            Container parent = dock.container.parent.parent;
                            Container working = null;
                            if (dock.container.parent.parent.splits[0] == dock.container.parent)
                                working = dock.container.parent.parent.splits[0] = dock.container.parent.splits[1];
                            else working = dock.container.parent.parent.splits[1] = dock.container.parent.splits[1];
                            working.parent = parent;
                            working.size = dock.container.parent.size;
                        }
                        else
                        {
                            toDelete = dock.container.parent.splits[1];
                            Container parent = dock.container.parent.parent;
                            Container working = null;
                            if (dock.container.parent.parent.splits[0] == dock.container.parent)
                                working = dock.container.parent.parent.splits[0] = dock.container.parent.splits[0];
                            else working = dock.container.parent.parent.splits[1] = dock.container.parent.splits[0];
                            working.parent = parent;
                            working.size = dock.container.parent.size;
                        }
                    }
                    for (int i = 0; i < m_containers.Count; i++)
                    {
                        if (toDelete == m_containers[i])
                        {
                            //delete m_containers[i];
                            //m_containers.erase(m_containers.begin() + i);
                            // Trans:
                            m_containers.RemoveAt(i);
                        }
                        if (m_containers.Count > 1 && parentToDelete == m_containers[i])
                        {
                            //delete m_containers[i];
                            //m_containers.erase(m_containers.begin() + i);
                            // Trans:
                            m_containers.RemoveAt(i);
                        }
                        if (m_containers.Count > 1 && toDelete == m_containers[i])
                        {
                            //delete m_containers[i];
                            //m_containers.erase(m_containers.begin() + i);
                            // Trans:
                            m_containers.RemoveAt(i);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void updateAndDraw(Vector2 dockspaceSize)
        {
            uint idgen = 0;

            float tabbarHeight = 20;

            void renderContainer(Container container, Vector2 size, Vector2 cursorPos) {
                Vector2 calculatedSize = size;
                Vector2 calculatedCursorPos = cursorPos;

                idgen++;

                string idname = "Dock##";
                idname += idgen;

                calculatedSize.Y -= tabbarHeight;

                float splitterButtonWidth = 4;
                float splitterButtonWidthHalf = splitterButtonWidth / 2;

                if (container.splits[0] == null && container != m_container)
                {
                    _renderTabBar(container, calculatedSize, cursorPos);
                    cursorPos.Y += tabbarHeight;

                    ImGui.SetCursorPos(cursorPos);
                    Vector2 screenCursorPos = ImGui.GetCursorScreenPos();
                    screenCursorPos.Y -= tabbarHeight;

                    //ImGui.PushStyleColor(ImGuiCol.ChildWindowBg, new Vector4(.25f, .25f, .25f, 1));
                    // Trans:
                    ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(new Vector4(.25f, .25f, .25f, 1f)));
                    ImGui.BeginChild(idname, calculatedSize, false, ImGuiWindowFlags.AlwaysUseWindowPadding);
                    container.activeDock.drawFunction(calculatedSize);
                    container.activeDock.lastSize = calculatedSize;

                    ImGui.EndChild();
                    ImGui.PopStyleColor(1);
                }
                else
                {
                    Vector2 calculatedSize0 = size, calculatedSize1 = Vector2.Zero;

                    if (container.splits[1] != null)
                    {
                        float acontsizeX = container.splits[0].size != 0 ? container.splits[0].size :
                            container.splits[1].size != 0 ? size.X - container.splits[1].size - splitterButtonWidth : size.X / 2 - splitterButtonWidthHalf;
                        float acontsizeY = container.splits[0].size != 0 ? container.splits[0].size :
                            container.splits[1].size != 0 ? size.Y - container.splits[1].size - splitterButtonWidth : size.Y / 2 - splitterButtonWidthHalf;

                        float bcontsizeX = container.splits[0].size != 0 ? size.X - container.splits[0].size - splitterButtonWidth :
                            container.splits[1].size != 0 ? container.splits[1].size : size.X / 2 - splitterButtonWidthHalf;
                        float bcontsizeY = container.splits[0].size != 0 ? size.Y - container.splits[0].size - splitterButtonWidth :
                            container.splits[1].size != 0 ? container.splits[1].size : size.Y / 2 - splitterButtonWidthHalf;

                        calculatedSize0 = new Vector2(container.verticalSplit ? acontsizeX : size.X, !container.verticalSplit ? acontsizeY : size.Y);
                        calculatedSize1 = new Vector2(container.verticalSplit ? bcontsizeX : size.X, !container.verticalSplit ? bcontsizeY : size.Y);
                    }
                    if (container.splits[0] != null)
                    {
                        if (container.splits[0] == null)
                            size.X = 1;
                        renderContainer(container.splits[0], calculatedSize0, calculatedCursorPos);
                        if (container.verticalSplit)
                            calculatedCursorPos.X = calculatedSize0.X + calculatedCursorPos.X + splitterButtonWidth;
                        else
                        {
                            calculatedCursorPos.Y = calculatedSize0.Y + calculatedCursorPos.Y + splitterButtonWidth;
                        }
                    }
                    Container thisContainer = container.splits[1];
                    if (container.splits[1] != null)
                    {
                        ImGui.SetCursorPosX(calculatedCursorPos.X - splitterButtonWidth);
                        ImGui.SetCursorPosY(calculatedCursorPos.Y - splitterButtonWidth);
                        string idnamesb = "##SplitterButton";
                        idnamesb += idgen++;
                        ImGui.InvisibleButton(idnamesb, new Vector2(
                            container.verticalSplit ? splitterButtonWidth : size.X + splitterButtonWidth,
                            !container.verticalSplit ? splitterButtonWidth : size.Y + splitterButtonWidth));

                        ImGui.SetItemAllowOverlap(); // This is to allow having other buttons OVER our splitter. 

                        if (ImGui.IsItemActive())
                        {
                            float mouse_delta = !container.verticalSplit ? ImGui.GetIO().MouseDelta.Y : ImGui.GetIO().MouseDelta.X;

                            if (container.splits[0].alwaysAutoResize != true)
                            {
                                _getMinSize(container.splits[0], out var minSize);
                                if (container.splits[0].size == 0)
                                    container.splits[0].size = container.verticalSplit ? calculatedSize1.X : calculatedSize1.Y;
                                if (container.splits[0].size + mouse_delta >= (container.verticalSplit ? minSize.X : minSize.Y))
                                    container.splits[0].size += mouse_delta;
                            }
                            else
                            {
                                _getMinSize(container.splits[1], out var minSize);
                                if (container.splits[1].size == 0)
                                    container.splits[1].size = container.verticalSplit ? calculatedSize1.X : calculatedSize1.Y;
                                if (container.splits[1].size - mouse_delta >= (container.verticalSplit ? minSize.X : minSize.Y))
                                    container.splits[1].size -= mouse_delta;
                            }
                        }

                        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
                        {
                            ImGui.SetMouseCursor(container.verticalSplit ? ImGuiMouseCursor.ResizeNS : ImGuiMouseCursor.ResizeEW);
                            // TODO: fixme
                            //SetCursor(LoadCursor(NULL, container.verticalSplit ? IDC_SIZEWE : IDC_SIZENS));
                        }

                        renderContainer(container.splits[1], calculatedSize1, calculatedCursorPos);
                    }
                }
            };

            Vector2 backup_pos = ImGui.GetCursorPos();
            renderContainer(m_container, dockspaceSize, backup_pos);
            ImGui.SetCursorPos(backup_pos);
        }

        public readonly List<Dock> m_docks = new List<Dock>();

        public readonly Container m_container = new Container();
        public  readonly List<Container> m_containers = new List<Container>();

        protected void _renderTabBar(Container container, Vector2 size, Vector2 cursorPos)
        {
            ImGui.SetCursorPos(cursorPos);

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(14, 3));
            foreach (var dock in container.docks)
            {
                string dockTitle = dock.title;
                if (dock.closeButton == true)
                    dockTitle += "  ";

                if (dock == container.activeDock)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(new Vector4(.25f, .25f, .25f, 1)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetColorU32(new Vector4(.25f, .25f, .25f, 1)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(new Vector4(.25f, .25f, .25f, 1)));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(new Vector4(.21f, .21f, .21f, 1)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetColorU32(new Vector4(.35f, .35f, .35f, 1)));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(new Vector4(.4f, .4f, .4f, 1)));
                }
                if (ImGui.Button(dockTitle, new Vector2(0, 20)))
                {
                    container.activeDock = dock;
                }

                ImGui.SameLine();
                ImGui.PopStyleColor(3);
            }
            ImGui.PopStyleVar();
        }
        protected bool _getMinSize(Container container, out Vector2 min)
        {
            min = Vector2.Zero;
            int begin = 0;
            if (container.splits[0] == null)
            {
                if (min.X < container.activeDock.minSize.X)
                    min.X = container.activeDock.minSize.X;
                if (min.Y < container.activeDock.minSize.Y)
                    min.Y = container.activeDock.minSize.Y;
                return true;
            }
            else
            {
                if (_getMinSize(container.splits[0], out min))
                {
                    if (container.splits[1] != null)
                    {
                        if (_getMinSize(container.splits[1], out min))
                        {
                            return true;
                        }
                    }
                };
            }

            return false;
        }

        //enum DockToAction
        //{
        //    eUndock, eDrag, eClose, eNull
        //};

        //Dock m_currentDockTo = null;
        //DockToAction m_currentDockToAction = eNull;
    };
}
