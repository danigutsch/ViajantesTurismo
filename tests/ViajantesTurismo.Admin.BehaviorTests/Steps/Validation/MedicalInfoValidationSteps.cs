namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Validation;

[Binding]
[Scope(Feature = "Medical Info Validation")]
public sealed class MedicalInfoValidationSteps(CustomerContext context)
{
    [When(@"I create medical info with allergies ""([^""]*)"" and additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithAllergiesAndAdditionalInfo(string allergies, string additionalInfo)
    {
        context.MedicalInfoResult = MedicalInfo.Create(allergies, additionalInfo);
    }

    [When(@"I create medical info with only allergies ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithOnlyAllergies(string allergies)
    {
        context.MedicalInfoResult = MedicalInfo.Create(allergies, null);
    }

    [When(@"I create medical info with only additional info ""([^""]*)""")]
    public void WhenICreateMedicalInfoWithOnlyAdditionalInfo(string additionalInfo)
    {
        context.MedicalInfoResult = MedicalInfo.Create(null, additionalInfo);
    }

    [When("I create medical info without any information")]
    public void WhenICreateMedicalInfoWithoutAnyInformation()
    {
        context.MedicalInfoResult = MedicalInfo.Create(null, null);
    }

    [When(@"I attempt to create medical info with allergies of (\d+) characters")]
    public void WhenIAttemptToCreateMedicalInfoWithAllergiesOfCharacters(int length)
    {
        context.MedicalInfoResult = MedicalInfo.Create(new string('A', length), null);
    }

    [When(@"I create medical info with allergies of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAllergiesOfCharacters(int length)
    {
        context.MedicalInfoResult = MedicalInfo.Create(new string('A', length), null);
    }

    [When(@"I attempt to create medical info with additional info of (\d+) characters")]
    public void WhenIAttemptToCreateMedicalInfoWithAdditionalInfoOfCharacters(int length)
    {
        context.MedicalInfoResult = MedicalInfo.Create(null, new string('B', length));
    }

    [When(@"I create medical info with additional info of (\d+) characters")]
    public void WhenICreateMedicalInfoWithAdditionalInfoOfCharacters(int length)
    {
        context.MedicalInfoResult = MedicalInfo.Create(null, new string('B', length));
    }

    [When("I create medical info with fields containing extra whitespace")]
    public void WhenICreateMedicalInfoWithFieldsContainingExtraWhitespace()
    {
        context.MedicalInfoResult = MedicalInfo.Create("Peanuts,    Shellfish,    Dairy", "Requires    medication    daily");
    }

    [When("I attempt to create medical info with both fields exceeding maximum length")]
    public void WhenIAttemptToCreateMedicalInfoWithBothFieldsExceedingMaximumLength()
    {
        context.MedicalInfoResult = MedicalInfo.Create(new string('A', 501), new string('B', 501));
    }

    [Then("the medical info should be successfully created")]
    public void ThenTheMedicalInfoShouldBeSuccessfullyCreated()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.IsSuccess).ShouldBeTrue(context.MedicalInfoResult.Value.ErrorDetails?.Detail ?? "Creation failed");
    }

    [Then("the allergies should be empty")]
    public void ThenTheAllergiesShouldBeEmpty()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.Value.Allergies).ShouldBeNull();
    }

    [Then("the additional info should be empty")]
    public void ThenTheAdditionalInfoShouldBeEmpty()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.Value.AdditionalInfo).ShouldBeNull();
    }

    [Then("I should be informed that allergies cannot exceed 500 characters")]
    public void ThenIShouldBeInformedThatAllergiesCannotExceed500Characters()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.IsFailure).ShouldBeTrue("Expected failure but got success");
        var errors = context.MedicalInfoResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        (allErrors).ShouldContain("Allergies cannot exceed 500 characters.");
    }

    [Then("I should be informed that additional information cannot exceed 500 characters")]
    public void ThenIShouldBeInformedThatAdditionalInformationCannotExceed500Characters()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.IsFailure).ShouldBeTrue("Expected failure but got success");
        var errors = context.MedicalInfoResult.Value.ErrorDetails?.ValidationErrors;
        var allErrors = errors?.Values.SelectMany(e => e).ToList() ?? new List<string>();
        (allErrors).ShouldContain("Additional information cannot exceed 500 characters.");
    }

    [Then("all medical info fields should have normalized whitespace")]
    public void ThenAllMedicalInfoFieldsShouldHaveNormalizedWhitespace()
    {
        (context.MedicalInfoResult).ShouldNotBeNull();
        (context.MedicalInfoResult.Value.Value.Allergies).ShouldBe("Peanuts, Shellfish, Dairy");
        (context.MedicalInfoResult.Value.Value.AdditionalInfo).ShouldBe("Requires medication daily");
    }
}
