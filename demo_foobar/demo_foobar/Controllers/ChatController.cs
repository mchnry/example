using Mchnry.Flow;
using Mchnry.Flow.Diagnostics;
using Mchnry.Flow.Logic;
using Mchnry.Flow.Logic.Define;
using Mchnry.Flow.Work;
using Mchnry.Flow.Work.Define;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace demo_foobar.Controllers
{

    public class MessagePost
    {
        public string UserName { get; set; }
        public string Message { get; set; }
    }

    internal class PostMessageAction : IAction<MessagePost>
    {
        public ActionDefinition Definition => new ActionDefinition()
        {
            Id = "postMessage",
            Description = "Persists users message"
        };

        public async Task<bool> CompleteAsync(IEngineScope<MessagePost> scope, WorkflowEngineTrace trace, CancellationToken token)
        {
            //this is where we would persist
            return await Task.FromResult(true);
        }
    }

    internal class IsMessageCleanEvaluator : IRuleEvaluator<MessagePost>
    {
        public Evaluator Definition => new Evaluator()
        {
            Id = "isMessageClean",
            Description = "Determines if message is appropriate"
        };

        public async Task EvaluateAsync(IEngineScope<MessagePost> scope, LogicEngineTrace trace, IRuleResult result, CancellationToken token)
        {
            var model = scope.GetModel();
            bool bad = model.Message.IndexOf("badword") > -1;
            if (bad)
            {
                result.FailWithValidation(new Validation("badword", ValidationSeverity.Fatal, "Lifebuoy for you!"));
            }
            else result.Pass();
        }
    }

    public class EvaluatorFactory
    {
        internal IRuleEvaluator<MessagePost> IsMessageClean => new IsMessageCleanEvaluator();
    }
    public class ActionFactory
    {
        internal IAction<MessagePost> PostMessage = new PostMessageAction();
    }

    public class BuilderFactory
    {
        public ActionFactory AF = new ActionFactory();
        public EvaluatorFactory EF = new EvaluatorFactory();
        internal IWorkflowBuilder<MessagePost> PostMessage => new WorkflowBuilder<MessagePost>(
                Builder<MessagePost>.CreateBuilder("postMessage")
                    .BuildFluent(todo => todo
                        .IfThenDo(
                            If => If.Rule(rule => rule.Eval(EF.IsMessageClean)),
                            Then => Then.Do(action => action.Do(AF.PostMessage))
                        )
                    )
            );
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {

        public BuilderFactory BF = new BuilderFactory();

        // POST api/chat
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MessagePost post, CancellationToken token)
        {
            var runner = Engine<MessagePost>
                .CreateEngine()
                .StartFluent(BF.PostMessage, post);

            var inspector = await runner.ExecuteAutoFinalizeAsync(token);

            if (!inspector.Validations.ResolveValidations())
            {
                return this.Conflict(inspector.Validations);
            }
            else
            {
                return this.Ok(inspector.Validations);
            }

        }


    }
}
