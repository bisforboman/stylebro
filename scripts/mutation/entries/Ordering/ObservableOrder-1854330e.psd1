# BRO1001: only the members reflection reads by position keep their order
@{
    File = 'src/StyleBro.Analyzers/Ordering/ObservableOrder.cs'
    Find = 'bool IsRead(int i) => everyMember || '
    Replace = 'bool IsRead(int i) => true || '
    Tests = 'MemberOrderingTests.MembersOtherThanTheOnesReadByPosition'
}
