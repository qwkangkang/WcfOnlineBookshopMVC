Imports System.IO

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IService1" in both code and config file together.
<ServiceContract()>
Public Interface IPaymentService

    <OperationContract()>
    Function GetData(ByVal value As String) As String

    <OperationContract()>
    Function RetrievePaymentDetail(paymentDetail As Stream) As Boolean


    <OperationContract()>
    Function RetrievePaymentPageInfo(userID As Integer, bookID As String, quantity As Integer) As String


    <OperationContract()>
    Function RetrievePaymentPageInfoEmail(userID As Integer) As String


    <OperationContract()>
    Function RetrieveCartPageInfo(userID) As List(Of ModelLibrary.BookCartCombined)


    <OperationContract()>
    Function CalculateTotalPrice(items As List(Of ModelLibrary.BookCartCombined)) As Decimal


    <OperationContract()>
    Function DeleteZeroCartItem(userID As Integer, bookID As Integer) As Boolean

    <OperationContract()>
    Function SaveCartChangesToDB(userID As Integer, quantity As Integer, bookID As Integer) As Boolean


    <OperationContract()>
    Function CheckBookQuantity(bookID As Integer) As Integer


    <OperationContract()>
    Function RetrieveWishlistPageInfo(userID As Integer) As List(Of ModelLibrary.BookWishlistCombined)


    <OperationContract()>
    Function RemoveFromWishlist(userID As Integer, bookID As Integer) As Integer
    '<OperationContract()>
    'Function GetDataUsingDataContract(ByVal composite As CompositeType) As CompositeType

    ' TODO: Add your service operations here

End Interface

' Use a data contract as illustrated in the sample below to add composite types to service operations.
' You can add XSD files into the project. After building the project, you can directly use the data types defined there, with the namespace "WcfOnlineBookshopMVC.ContractType".

<DataContract()>
Public Class CompositeType

    <DataMember()>
    Public Property BoolValue() As Boolean

    <DataMember()>
    Public Property StringValue() As String

End Class
