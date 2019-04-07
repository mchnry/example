using System;
using System.Threading;
using System.Threading.Tasks;
using Mchnry.Flow;
using Mchnry.Flow.Diagnostics;
using Mchnry.Flow.Logic;
using Mchnry.Flow.Logic.Define;
using Mchnry.Flow.Work;
using Mchnry.Flow.Work.Define;

namespace Beginner
{
    class Program
    {

        #region Model

        /// <summary>
        /// Model for beginner example.  Trite, I know.
        /// </summary>
        internal class ExampleModel
        {
            /// <summary>
            /// Estimate amount
            /// </summary>
            public decimal EstimateAmount { get; set; } = 0;
        }
        #endregion

        #region Evaluators
        internal class DoesCustomerDecideEvaluator : IRuleEvaluator<Program.ExampleModel>
        {

            public Evaluator Definition => new Evaluator()
            {
                Description = "Evaluates if model.estimate exceeds threashold",
                Id = "threasholdTest"
            };

            public async Task EvaluateAsync(IEngineScope<ExampleModel> scope, LogicEngineTrace trace, IRuleResult result, CancellationToken token)
            {
                result.SetResult(scope.GetModel().EstimateAmount > (decimal)500.00);
            }
        }

        #endregion

        #region Actions
        internal class SendRequestToCustomerAction : IAction<Program.ExampleModel>
        {
            public ActionDefinition Definition => new ActionDefinition()
            {
                Description = "Sends request to customer for decision",
                Id = "sendDecisionRequest"
            };

            public async Task<bool> CompleteAsync(IEngineScope<ExampleModel> scope, WorkflowEngineTrace trace, CancellationToken token)
            {
                Console.WriteLine($"Sending Request to Customer since {scope.GetModel().EstimateAmount} exceeds threshold");
                return await Task.FromResult(true);
            }
        }

        internal class AutoApproveAction : IAction<Program.ExampleModel>
        {
            public ActionDefinition Definition => new ActionDefinition()
            {
                Description = "Auto approve",
                Id = "autoApprove"
            };

            public async Task<bool> CompleteAsync(IEngineScope<ExampleModel> scope, WorkflowEngineTrace trace, CancellationToken token)
            {
                Console.WriteLine($"Auto approve since {scope.GetModel().EstimateAmount} is under threshold");
                return await Task.FromResult(true);
            }
        }

        #endregion

        #region BuilderFactory
        internal class BuilderFactory : IWorkflowBuilderFactory
        {
            public IWorkflowBuilder<T> GetWorkflow<T>(string workflowId)
            {
                IWorkflowBuilder<T> toReturn = default;

                
                
           
                switch(workflowId)
                {
                    case "example":
                        toReturn = (IWorkflowBuilder<T>)new WorkflowBuilder<ExampleModel>( Builder<ExampleModel>
                            .CreateBuilder(workflowId)
                            .BuildFluent(todo => todo
                                .IfThenDo(
                                    If => If.RuleIsTrue(e => e.Eval(new DoesCustomerDecideEvaluator()).IsTrue()),
                                    Then => Then.Do(a => a.Do(new SendRequestToCustomerAction()))
                                ).Else(Else => Else.Do(a => a.Do(new AutoApproveAction())))

                               
                        )
                        );
                        break;
                }

                return toReturn;
            }
        }
        #endregion

        public static async Task Main(string[] args)
        {
            /*
            Simple example
            Synopsis:   Customer approval process allows us to make a decision on their behalf in certain conditions
                        otheriwse, we need to allow them to review
            */

            //Define the flow.  This is done in a builder factory
            Console.WriteLine("Example of under threashold");
            var engine = Engine<ExampleModel>.CreateEngine()
                .SetWorkflowDefinitionFactory(new BuilderFactory())
                .Start("example", new ExampleModel() { EstimateAmount = (decimal)500.00 });
            var result = await engine.ExecuteAutoFinalizeAsync(new CancellationToken());

            Console.WriteLine("Example of over threashold");
            engine = Engine<ExampleModel>.CreateEngine()
                .SetWorkflowDefinitionFactory(new BuilderFactory())
                .Start("example", new ExampleModel() { EstimateAmount = (decimal)1000.00 });
            result = await engine.ExecuteAutoFinalizeAsync(new CancellationToken());



            Console.WriteLine("Enter to Exit");
            Console.ReadLine();


        }
    }


}
