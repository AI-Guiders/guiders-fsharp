namespace AIGuiders.Platform.Modeling.Gdl.Parse.CockpitLogic

open AIGuiders.Platform.Modeling.Gdl.Expression

[<CLIMutable>]
type CockpitRule =
    { Id: string
      When: ExprNode
      Emit: string
      Severity: string }

[<CLIMutable>]
type CockpitProjector =
    { Id: string
      Source: string
      Target: string }

[<CLIMutable>]
type CockpitRuleGraph =
    { Planet: string
      Rules: CockpitRule list
      Projectors: CockpitProjector list }
