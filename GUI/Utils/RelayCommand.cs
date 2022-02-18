using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InfiniteVariantTool.GUI
{
    public abstract class RelayCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public abstract bool CanExecute(object? parameter);

        public abstract void Execute(object? parameter);

        public bool IsExecuting { get; protected set; }
    }

    public class SyncRelayCommand : RelayCommand
    {
        private Action<object?> execute;
        private Func<object?, bool>? canExecute;
        private Action<Exception>? onError;

        public SyncRelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null, Action<Exception>? onError = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            this.onError = onError;
        }

        public override bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public override void Execute(object? parameter)
        {
            try
            {
                IsExecuting = true;
                execute(parameter);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                throw;
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }

    public class AsyncRelayCommand : RelayCommand
    {
        private Func<object?, Task> execute;
        private Func<object?, bool>? canExecute;
        private Action<Exception>? onError;

        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null, Action<Exception>? onError = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            this.onError = onError;
        }

        public override bool CanExecute(object? parameter)
        {
            return !IsExecuting && (canExecute?.Invoke(parameter) ?? true);
        }

        public override async void Execute(object? parameter)
        {
            await ExecuteAsync(parameter);
        }

        public async Task ExecuteAsync(object? parameter)
        {
            IsExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await execute(parameter);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                throw;
            }
            finally
            {
                IsExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}
