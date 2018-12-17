﻿using grrui.Model;
using grrui.UI;
using RepoZ.Ipc;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui;

namespace grrui
{
	class Program
	{
        private const int BUTTON_BORDER = 4; // 2 chars to the left, 2 to the right
        private const int BUTTON_DISTANCE = 1;

		private static RepoZIpcClient _client;
		private static ListView _repositoryList;
		private static RepositoriesView _repositoriesView;

		static void Main(string[] args)
		{
			_client = new RepoZIpcClient();
			var answer = _client.GetRepositories();

			if (answer.Repositories == null)
			{
				Console.WriteLine(answer.Answer);
				return;
			}

			if (answer.Repositories.Length == 0)
			{
				Console.WriteLine("No repositories yet");
				return;
			}

			_repositoriesView = new RepositoriesView(answer.Repositories);

			Application.Init();

			var filterLabel = new Label(1, 1, "Filter: ");
			var filterField = new TextField("")
			{
				X = Pos.Right(filterLabel) + 2,
				Y = Pos.Top(filterLabel),
				Width = Dim.Fill(margin: 1),
			};
			filterField.Changed += FilterField_Changed;

			_repositoryList = new ListView(_repositoriesView.Repositories)
			{
				X = Pos.Left(filterLabel),
				Y = Pos.Bottom(filterLabel) + 1,
				Width = Dim.Fill(margin: 1),
				Height = Dim.Fill() - 2
			};

			var win = new KeyPreviewWindow("grr: Git repositories of RepoZ");
			win.Add(filterLabel);
			win.Add(filterField);
			win.Add(_repositoryList);

            var buttonX = Pos.Left(filterLabel);
            var navigationButton = new Button("Navigate")
			{
				Clicked = Navigate,
				X = buttonX,
				Y = Pos.AnchorEnd(1),
				CanFocus = false
			};

            buttonX = buttonX + navigationButton.Text.Length + BUTTON_BORDER + BUTTON_DISTANCE;
            var browseButton = new Button("Browse")
			{
				Clicked = Browse,
				X = buttonX,
				Y = Pos.AnchorEnd(1),
				CanFocus = false
			};

            buttonX = buttonX + browseButton.Text.Length + BUTTON_BORDER + BUTTON_DISTANCE;
            var copyButton = new Button("Copy path")
			{
				Clicked = CopyPath,
                X = buttonX,
                Y = Pos.AnchorEnd(1),
				CanFocus = false
			};

			var quitButton = new Button("Quit")
			{
				Clicked = Application.RequestStop,
				X = Pos.AnchorEnd(8 + 1),
				Y = Pos.AnchorEnd(1),
				CanFocus = false
			};

			win.Add(navigationButton, browseButton, copyButton, quitButton);

			win.DefineKeyAction(Key.Enter, () => win.SetFocus(_repositoryList));
			win.DefineKeyAction(Key.Esc, () =>
			{
				if (filterField.HasFocus)
				{
					filterField.Text = "";
					FilterField_Changed(filterField, EventArgs.Empty);
				}
				else
				{
					win.SetFocus(filterField);
				}
			});

			Application.Top.Add(win);
			Application.Run();
		}

		private static void Navigate()
		{
			ExecuteOnSelectedRepository(r =>
			{
				var command = $"cd \"{r.SafePath}\"";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // type the path into the console which is hosting grrui.exe to change to the directory
                    grr.ConsoleExtensions.WriteConsoleInput(Process.GetCurrentProcess(), command, waitMilliseconds: 1000);
                }
                else
                {
                    // TODO
                    TimelyMessage.ShowMessage("Not implemented. The command to navigate was copied to the clipboard.", TimeSpan.FromMilliseconds(100));
                    TextCopy.Clipboard.SetText(command);
                }

				Application.RequestStop();
			});
		}

		private static void Browse()
		{
			ExecuteOnSelectedRepository(r =>
			{
				Process.Start(new ProcessStartInfo(r.SafePath) { UseShellExecute = true });
			});
		}

		private static void CopyPath()
		{
			ExecuteOnSelectedRepository(r =>
			{
				TextCopy.Clipboard.SetText(r.SafePath);
				TimelyMessage.ShowMessage("Path copied to clipboard", TimeSpan.FromMilliseconds(100));
			});
		}

		private static void ExecuteOnSelectedRepository(Action<Repository> action)
		{
			var repositories = _repositoriesView?.Repositories;
			if (repositories?.Length > _repositoryList.SelectedItem)
			{
				var current = repositories[_repositoryList.SelectedItem];
				action(current.Repository);
			}
		}

		private static void FilterField_Changed(object sender, EventArgs e)
		{
			_repositoriesView.Filter = (sender as TextField)?.Text?.ToString() ?? "";
			_repositoryList.SetSource(_repositoriesView.Repositories);
		}
	}
}