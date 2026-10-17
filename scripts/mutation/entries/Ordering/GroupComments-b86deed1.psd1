# BRO1001 fields_first: comments above other members are no group comments (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Ordering/GroupComments.cs'
    Find = 'if (!fieldsFirst && '
    Replace = 'if ('
    Tests = 'MemberOrderingTests.FieldsFirst_SortsATypeWhoseFirstMemberHasAComment'
}
