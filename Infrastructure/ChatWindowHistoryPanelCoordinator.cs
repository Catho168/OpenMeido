using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OpenMeido.Helpers;
using OpenMeido.Models;

namespace OpenMeido.Infrastructure
{
    public sealed class ChatWindowHistoryPanelCoordinator
    {
        private const double ExpandedHeight = 200;
        private const string ExpandedIcon = "📂";
        private const string CollapsedIcon = "📁";

        private readonly Window _owner;
        private readonly FrameworkElement _historyPanelHost;
        private readonly Border _historyPanel;
        private readonly Panel _historyItemsPanel;
        private readonly TextBlock _historyToggleIcon;
        private readonly Func<IEnumerable<ChatSession>> _getSavedSessions;
        private readonly Action<ChatSession> _loadSession;
        private readonly Action<string> _deleteSession;
        private readonly Action _updateCurrentSessionTitle;
        private readonly Func<ChatSession, bool> _confirmDeleteSession;
        private readonly Action<Border, double, Action> _animatePanel;
        private readonly Action _collapseMcpStatusPanel;
        private bool _isExpanded;

        public ChatWindowHistoryPanelCoordinator(
            Window owner,
            Border historyPanel,
            Panel historyItemsPanel,
            TextBlock historyToggleIcon,
            Func<IEnumerable<ChatSession>> getSavedSessions,
            Action<ChatSession> loadSession,
            Action<string> deleteSession,
            Action updateCurrentSessionTitle,
            Func<ChatSession, bool> confirmDeleteSession = null,
            Action<Border, double, Action> animatePanel = null,
            FrameworkElement historyPanelHost = null,
            Action collapseMcpStatusPanel = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(historyPanel);
            ArgumentNullException.ThrowIfNull(historyItemsPanel);
            ArgumentNullException.ThrowIfNull(historyToggleIcon);
            ArgumentNullException.ThrowIfNull(getSavedSessions);
            ArgumentNullException.ThrowIfNull(loadSession);
            ArgumentNullException.ThrowIfNull(deleteSession);
            ArgumentNullException.ThrowIfNull(updateCurrentSessionTitle);

            _owner = owner;
            _historyPanelHost = historyPanelHost ?? historyPanel;
            _historyPanel = historyPanel;
            _historyItemsPanel = historyItemsPanel;
            _historyToggleIcon = historyToggleIcon;
            _getSavedSessions = getSavedSessions;
            _loadSession = loadSession;
            _deleteSession = deleteSession;
            _updateCurrentSessionTitle = updateCurrentSessionTitle;
            _confirmDeleteSession = confirmDeleteSession ?? ConfirmDeleteSession;
            _animatePanel = animatePanel ?? AnimatePanel;
            _collapseMcpStatusPanel = collapseMcpStatusPanel ?? (() => { });

            SetExpanded(false);
            ApplyCollapsedHostState();
        }

        public void Initialize()
        {
            ApplyCollapsedHostState();
            Refresh();
            _updateCurrentSessionTitle();
        }

        public void Toggle()
        {
            if (_isExpanded)
            {
                Collapse();
            }
            else
            {
                Expand();
            }
        }

        public void CollapseIfExpanded()
        {
            if (_isExpanded)
            {
                Collapse();
                return;
            }

            SetExpanded(false);
            ApplyCollapsedHostState();
        }

        public void Refresh()
        {
            _historyItemsPanel.Children.Clear();

            foreach (var session in _getSavedSessions() ?? Array.Empty<ChatSession>())
            {
                _historyItemsPanel.Children.Add(CreateHistoryItem(session));
            }
        }

        private void Expand()
        {
            _collapseMcpStatusPanel();
            SetExpanded(true);
            ApplyExpandedHostState();
            _animatePanel(_historyPanel, ExpandedHeight, Refresh);
        }

        private void Collapse()
        {
            SetExpanded(false);
            _historyPanelHost.IsHitTestVisible = false;
            _animatePanel(_historyPanel, 0, ApplyCollapsedHostState);
        }

        private void SetExpanded(bool isExpanded)
        {
            _isExpanded = isExpanded;
            _historyToggleIcon.Text = isExpanded ? ExpandedIcon : CollapsedIcon;
        }

        private void ApplyExpandedHostState()
        {
            _historyPanelHost.Visibility = Visibility.Visible;
            _historyPanelHost.IsHitTestVisible = true;
        }

        private void ApplyCollapsedHostState()
        {
            _historyPanelHost.Visibility = Visibility.Collapsed;
            _historyPanelHost.IsHitTestVisible = false;
        }

        private Border CreateHistoryItem(ChatSession session)
        {
            return ChatHistoryItemElementFactory.Create(
                session.Title,
                () => _loadSession(session),
                () => DeleteSession(session));
        }

        private void DeleteSession(ChatSession session)
        {
            if (!_confirmDeleteSession(session))
            {
                return;
            }

            _deleteSession(session.SessionId);
            Refresh();
            _updateCurrentSessionTitle();
        }

        private bool ConfirmDeleteSession(ChatSession session)
        {
            var result = MessageBox.Show(
                _owner,
                $"确定要删除对话 \"{session.Title}\" 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private static void AnimatePanel(Border panel, double targetHeight, Action onCompleted)
        {
            double currentHeight = panel.Height;
            if (double.IsNaN(currentHeight))
            {
                currentHeight = 0;
            }

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseInOut }
            };

            if (onCompleted != null)
            {
                EventHandler renderingHandler = null;
                renderingHandler = (_, __) =>
                {
                    if (Math.Abs(panel.Height - targetHeight) < 0.5)
                    {
                        CompositionTarget.Rendering -= renderingHandler;
                        onCompleted();
                    }
                };

                CompositionTarget.Rendering += renderingHandler;
            }

            panel.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }
    }
}