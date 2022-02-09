using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
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
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter(); //экземпляр класса 

                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element,
                    groupPickFilter, //интерфейс, который реализовывает два буловых метода AllowElement и AllowReference, которые возвращают тру, если 
                                     //пользователь выбрал правильный элемент в модели. Передаем сюда экземпляр созданного нами ниже класса GroupPickFilter
                    "Выберите группу объектов"); //пользователь выбрал объект и получил ссылку на него

                Element element = doc.GetElement(reference); //преобразовали в Element - родительский класс для всех объектов RevitAPI, они унаследованы от него

                //преобразовываем объект из базового типа Element в Group

                //Group group = (Group)element; //это явное приведение, которое может дать исключение, если пользователь кликнул не по Group
                Group group = element as Group; //предпочтительно так преобразовывать

                //вокруг любого элемента в ревите можно построить ограничивающую рамку BoundingBox, 

                XYZ groupCenter = GetElementCenter(element); //получили центр группы
                Room room = GetRoomByPoint(doc, groupCenter); //выбираем комнату, где находится исходная группа объектов
                XYZ roomCenter = GetElementCenter(room); //находим центр комнаты
                XYZ offset = groupCenter - roomCenter; //определяем смещение центра группы относительно центра комнаты


                
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку"); //попросим пользователя выбрать точку вставки
                Room newRoom = GetRoomByPoint(doc, point); //проверили принадлежит ли точка комнате
                XYZ newRoomCenter = GetElementCenter(newRoom); //нашли центр этой комнаты
                point = newRoomCenter + offset; 




                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");

                doc.Create.PlaceGroup(point, group.GroupType); //все действия, касающиеся создания чего либо в ревите относятся к пространству имен Autodesk.Revit.Creation

                transaction.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException) //если пользователь нажал esc во время выполнения програмы
            {
                return Result.Cancelled; //вернем отмену
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;

            }
            return Result.Succeeded;
        }
        public XYZ GetElementCenter (Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null); //метод принимает любой элемент (группа, комната) и возвращает центр
            return (bounding.Max + bounding.Min) / 2;
            //Min - левый нижний дальний угол
            //Max - правый верхний ближний угол
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach(Element e in collector)
            {
                Room room = e as Room;
                if (room !=null)
                {
                    if (room.IsPointInRoom(point)) // IsPointInRoom этот метод возвращает тру или фолс если точка попадает в комнату
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }
}
public class GroupPickFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
            return true;
        else
            return false;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return false;
    }
}
