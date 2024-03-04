Imports System.IO
Imports System.Xml
Imports System.Security.Cryptography
Imports System.Text
Imports System.Xml.Serialization

' NOTE: You can use the "Rename" command on the context menu to change the class name "Service1" in both code and config file together.
Public Class PaymentService
    Implements IPaymentService

    Private ReadOnly dal As PaymentDAL.PaymentCRUD

    Public Sub New()
        dal = New PaymentDAL.PaymentCRUD
    End Sub

    Private Shared ReadOnly preSharedEncryptionKey As Byte() = {
    &H12, &H34, &H56, &H78, &HAB, &HCD, &HEF, &H12,
    &H34, &H56, &H78, &HAB, &HCD, &HEF, &H12, &H34,
    &H56, &H78, &HAB, &HCD, &HEF, &H12, &H34, &H56,
    &H78, &HAB, &HCD, &HEF, &H12, &H34, &H56, &H78
}

    Private Shared ReadOnly preSharedInitializationVector As Byte() = {
        &HAB, &HCD, &HEF, &H12, &H34, &H56, &H78, &HAB,
        &HCD, &HEF, &H12, &H34, &H56, &H78, &HAB, &HCD
    }

    Public Function GetData(ByVal value As String) As String Implements IPaymentService.GetData
        Return String.Format("You entered: {0}", value)
    End Function

    Public Function RetrievePaymentDetail(encryptedContentStream As Stream) As Boolean Implements IPaymentService.RetrievePaymentDetail
        Dim success As Boolean = True
        Dim key As Byte() = Encoding.UTF8.GetBytes("EncryptionKey")

        Dim encryptedContent As Byte()
        Using memoryStream As New MemoryStream()
            encryptedContentStream.CopyTo(memoryStream)
            encryptedContent = memoryStream.ToArray()
        End Using

        ' Extract the IV from the encrypted data (Assuming the IV is the first 16 bytes)
        Dim iv As Byte() = New Byte(15) {} ' 16 bytes for AES-128 (Change to 32 bytes for AES-256)
        Array.Copy(encryptedContent, iv, iv.Length)

        ' Decrypt the remaining encrypted content (excluding the IV) using the key and IV
        Dim encryptedDataWithoutIV As Byte() = New Byte(encryptedContent.Length - iv.Length - 1) {}
        Array.Copy(encryptedContent, iv.Length, encryptedDataWithoutIV, 0, encryptedDataWithoutIV.Length)

        Dim paymentDetail As String = DecryptAes(encryptedDataWithoutIV, key, iv)


        ' Remove the XML declaration (<?xml ... ?>) if present
        Dim startIndex As Integer = paymentDetail.IndexOf("<Payment>")
        If startIndex >= 0 Then
            Dim xmlContent As String = paymentDetail.Substring(startIndex)

            Dim xmlDoc As New XmlDocument()
            xmlDoc.LoadXml(xmlContent)

            Dim rootElement As XmlElement = xmlDoc.DocumentElement
            Dim contact As String = rootElement.SelectSingleNode("Contact").InnerText
            Dim method As String = rootElement.SelectSingleNode("Method").InnerText
            Dim location As String = rootElement.SelectSingleNode("Location").InnerText
            Dim totalPrice As Decimal = Decimal.Parse(rootElement.SelectSingleNode("TotalPrice").InnerText)
            Dim deliveryFee As Decimal = Decimal.Parse(rootElement.SelectSingleNode("DeliveryFee").InnerText)
            Dim dateTimePayment As DateTime = DateTime.Parse(rootElement.SelectSingleNode("DateTimePayment").InnerText)
            Dim paymentAmount As Decimal = Decimal.Parse(rootElement.SelectSingleNode("PaymentAmount").InnerText)
            Dim country As String = rootElement.SelectSingleNode("Country").InnerText
            Dim fname As String = rootElement.SelectSingleNode("Fname").InnerText
            Dim lname As String = rootElement.SelectSingleNode("Lname").InnerText
            Dim address As String = rootElement.SelectSingleNode("Address").InnerText
            Dim postcode As String = rootElement.SelectSingleNode("Postcode").InnerText
            Dim city As String = rootElement.SelectSingleNode("City").InnerText
            Dim state As String = rootElement.SelectSingleNode("State").InnerText
            Dim phone As String = rootElement.SelectSingleNode("Phone").InnerText
            Dim userID As Integer = Integer.Parse(rootElement.SelectSingleNode("UserID").InnerText)
            Dim bookIDBuyNow As Integer
            Dim quantityBuyNow As Integer
            If Not rootElement.SelectSingleNode("BookIDBuyNow").InnerText Is "" Then
                bookIDBuyNow = Integer.Parse(rootElement.SelectSingleNode("BookIDBuyNow").InnerText)
                quantityBuyNow = Integer.Parse(rootElement.SelectSingleNode("QuantityBuyNow").InnerText)
            End If

            Dim modelOrder As New ModelLibrary.BookOrder
            modelOrder.paymentDateTime = dateTimePayment
            modelOrder.userID = userID
            modelOrder.orderContact = contact
            modelOrder.paymentAmount = totalPrice
            modelOrder.orderDeliveryMethod = method
            modelOrder.orderDeliveryFee = deliveryFee
            modelOrder.orderLocation = location

            success = dal.InsertOrder(modelOrder)
            If success Then
                Dim modelPaymentDetail As New ModelLibrary.PaymentDetailViewModel
                modelPaymentDetail.dateTimePayment = dateTimePayment
                modelPaymentDetail.userID = userID
                modelPaymentDetail.totalPrice = paymentAmount
                modelPaymentDetail.country = country
                modelPaymentDetail.fname = fname
                modelPaymentDetail.lname = lname
                modelPaymentDetail.address = address
                modelPaymentDetail.postcode = postcode
                modelPaymentDetail.city = city
                modelPaymentDetail.phone = phone
                success = dal.InsertPayment(modelPaymentDetail)
            End If


            'If String.IsNullOrEmpty(bookIDBuyNow) Or String.IsNullOrEmpty(quantityBuyNow) Then
            If bookIDBuyNow = 0 Or quantityBuyNow = 0 Then
                If success Then
                    Dim modelBookATC As New ModelLibrary.OrderDetail
                    modelBookATC.bookID = bookIDBuyNow
                    modelBookATC.odQuantity = quantityBuyNow
                    modelBookATC.orderDateTime = dateTimePayment
                    modelBookATC.userID = userID

                    success = dal.UpdateBookATC(modelBookATC)
                End If
                If success Then
                    success = dal.UpdateCart(userID)
                End If

            Else
                If success Then
                    Dim modelBookBN As New ModelLibrary.OrderDetail
                    modelBookBN.userID = userID
                    modelBookBN.bookID = bookIDBuyNow
                    modelBookBN.odQuantity = quantityBuyNow
                    success = dal.UpdateBookBN(modelBookBN)
                End If
                If success Then
                    Dim modelBookDetailBN As New ModelLibrary.OrderDetail
                    modelBookDetailBN.userID = userID
                    modelBookDetailBN.orderDateTime = dateTimePayment
                    modelBookDetailBN.bookID = bookIDBuyNow
                    modelBookDetailBN.odQuantity = quantityBuyNow
                    success = dal.InsertOrderDetail(modelBookDetailBN)
                End If

            End If

        End If

        Return success
    End Function

    Private Function DecryptAes(ByVal encryptedData As Byte(), ByVal key As Byte(), ByVal iv As Byte()) As String

        Dim derivedKey As Byte() = DeriveKey(key)
        Using aes As Aes = aes.Create()
            aes.Key = derivedKey
            aes.IV = iv

            Using memoryStream As New MemoryStream()
                Using cryptoStream As New CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write)
                    cryptoStream.Write(encryptedData, 0, encryptedData.Length)
                    cryptoStream.FlushFinalBlock()
                    Dim decryptedBytes As Byte() = memoryStream.ToArray()
                    Return Encoding.UTF8.GetString(decryptedBytes)
                End Using
            End Using
        End Using

    End Function

    Private Function DeriveKey(key As Byte()) As Byte()
        Using sha256 As SHA256 = sha256.Create()
            Return sha256.ComputeHash(key)
        End Using
    End Function

    Public Function RetrievePaymentPageInfo(userID As Integer, bookID As String, quantity As Integer) As String Implements IPaymentService.RetrievePaymentPageInfo
        Dim items As New List(Of ModelLibrary.BookCartCombined)()

        If String.IsNullOrEmpty(bookID) Or String.IsNullOrEmpty(quantity) Then
            'add to cart method
            items = dal.SearchBookCartResult(userID)

        Else
            'buy now method
            items = dal.SearchBookBNResult(userID, bookID, quantity)

        End If

        ' Serialize the items list
        Dim serializedData As Byte() = SerializeItems(items)

        ' Encrypt the serialized data using AES encryption
        Dim encryptedData As String = EncryptData(serializedData)

        Return encryptedData

    End Function

    Public Shared Function EncryptData(data As Byte()) As String

        Using rijAlg As New RijndaelManaged()
            rijAlg.Key = preSharedEncryptionKey
            rijAlg.IV = preSharedInitializationVector
            rijAlg.Mode = CipherMode.CBC
            rijAlg.Padding = PaddingMode.PKCS7

            Using encryptor As ICryptoTransform = rijAlg.CreateEncryptor()
                Dim encryptedBytes As Byte() = encryptor.TransformFinalBlock(data, 0, data.Length)
                Return Convert.ToBase64String(encryptedBytes)
            End Using
        End Using
    End Function

    ' Serialize the list of BookCartCombined into a byte array
    Private Function SerializeItems(items As List(Of ModelLibrary.BookCartCombined)) As Byte()
        ' Code to serialize the items list to a byte array (e.g., using BinaryFormatter, XML serialization, JSON serialization, etc.)
        ' Return the serialized byte array
        Using memoryStream As New MemoryStream()
            Dim serializer As New XmlSerializer(GetType(List(Of ModelLibrary.BookCartCombined)))
            serializer.Serialize(memoryStream, items)
            Return memoryStream.ToArray()
        End Using
    End Function


    Public Function RetrievePaymentPageInfoEmail(userID As Integer) As String Implements IPaymentService.RetrievePaymentPageInfoEmail
        Return dal.SearchEmailPaymentPage(userID)
    End Function

    Public Function RetrieveCartPageInfo(userID) As List(Of ModelLibrary.BookCartCombined) Implements IPaymentService.RetrieveCartPageInfo

        Dim items As New List(Of ModelLibrary.BookCartCombined)()
        items = dal.SearchCartResult(userID)

        'Dim totalPrice As Decimal = CalculateTotalPrice(items)
        Return items

    End Function

    Public Function CalculateTotalPrice(items As List(Of ModelLibrary.BookCartCombined)) As Decimal Implements IPaymentService.CalculateTotalPrice
        Dim totalPrice As Decimal = 0

        For Each item As ModelLibrary.BookCartCombined In items
            totalPrice += item.bookPrice * item.cartNum
        Next

        Return totalPrice
    End Function

    Public Function DeleteZeroCartItem(userID As Integer, bookID As Integer) As Boolean Implements IPaymentService.DeleteZeroCartItem
        Return dal.DeleteCartItem(userID, bookID)
    End Function

    Public Function SaveCartChangesToDB(userID As Integer, quantity As Integer, bookID As Integer) As Boolean Implements IPaymentService.SaveCartChangesToDB
        Return dal.UpdateCartNum(userID, quantity, bookID)
    End Function

    Public Function CheckBookQuantity(bookID As Integer) As Integer Implements IPaymentService.CheckBookQuantity
        Return dal.SearchBookQuantity(bookID)
    End Function

    Public Function RetrieveWishlistPageInfo(userID As Integer) As List(Of ModelLibrary.BookWishlistCombined) Implements IPaymentService.RetrieveWishlistPageInfo
        Return dal.SearchWishlistResult(userID)
    End Function

    Public Function RemoveFromWishlist(userID As Integer, bookID As Integer) As Integer Implements IPaymentService.RemoveFromWishlist
        Return dal.UpdateNullWishlistItem(userID, bookID)
    End Function

    'Public Function GetDataUsingDataContract(ByVal composite As CompositeType) As CompositeType Implements IPaymentService.GetDataUsingDataContract
    '    If composite Is Nothing Then
    '        Throw New ArgumentNullException("composite")
    '    End If
    '    If composite.BoolValue Then
    '        composite.StringValue &= "Suffix"
    '    End If
    '    Return composite
    'End Function

End Class
