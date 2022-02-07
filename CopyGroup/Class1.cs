using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroup
{
    // RevitAPI состоит из двух базовых библиотек это:
    // RevitAPI - содержит классы используемые для доступа ко всему приложению Revit, документам, элементам и параметрам на уровне базы данных
    // RevitAPIUI содержит классы связанные с пользовательским инетерфейсом

    [TransactionAttribute(TransactionMode.Manual)] //помечаем атрибутом и ставим Manual, чтобы самому помечать где начинается и заканчивается транзакция

    public class CopyGroup : IExternalCommand //т.к. наше приложение должно работать как внешняя команда, нам нужно реализовать интерфейс
    {
        public Result Execute(ExternalCommandData commandData, //добираемся к ревит, документу, базе данных
            ref string message, //ref - передается по ссылке, если возвращаемый результат будет failure, то ревит отобразит ошибку
            ElementSet elements) // подсветит элементы, если произошла ошибка
        {
            //для доступа к документу используются классы UIApplication и Application
            //класс Application позволяет получить доступ к REvit как к таковому (версия ревита, локализация программы, активный пользователь,
            //можно програмно открыть документ, есть функции закрытия, открытия документа и делать что-то типо лога с документом
            //UIApplication - все что касается взаимодействия с пользователем в рамках целый программы, например при помощи методов, которые описаны в этом классе можно 
            //создать целую панель с кнопками

            //текущий открытый документ описывается классами Document и UIDocument
            //Document - база данных открытого документа. Через данный класс можно получать доступ к элементам, видам, создавать и удалять элементы.
            //UIDocument - это все что связанно с пользовательским интерфейсом в рамках документа. Дает возможность выбрать точки, объекты и т.д.
            //UIApplication -> из него получаем доступ к UIDocument -> а из него получаем доступ к Document

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов"); //пользователь выбрал объект и получил ссылку на него
            Element element = doc.GetElement(reference); //получили объект типа Element - родительский класс для всех объектов RevitAPI, они унаследованы от него

            //преобразовываем объект из базового типа Element в Group

            //Group group = (Group)element; //это явное приведение, которое может дать исключение, если пользователь кликнул не по Group
            Group group = element as Group; //предпочтительно так преобразовывать

            XYZ point = uiDoc.Selection.PickPoint("Выберите точку"); //попросим пользователя выбрать точку

            Transaction transaction = new Transaction(doc);
            transaction.Start("Копирование группы объектов");

            doc.Create.PlaceGroup(point, group.GroupType); //вообще все действия, которые касаются создания чего либо в документе ревит относятся к пространству имен Autodesk.Revit.Creation

            transaction.Commit();

            return Result.Succeeded;


        }
    }
}
